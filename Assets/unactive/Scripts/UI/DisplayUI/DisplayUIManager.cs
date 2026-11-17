using AYellowpaper.SerializedCollections;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DisplayUIManager : MonoBehaviour
{
    public enum ProfileState
    {
        Normal,
        Hit,
        Success,
        Hurt
    }

    [SerializeField]
    private CombatManager _combatManager;
    [SerializeField]
    private Player _player;
    private EverWind.Gameplay.PlayerServerTransmitter serverPlayer;
    private SkillButtonManager _skillButtonManager;

    public Image HpBar;
    public Image ProfileImage;
    private float _tempHpRatio;
    private EverWind.Gameplay.IGameplayEventSource gameplayEvents;
    private Coroutine profileRoutine;
    private SpriteRenderer _minimapSpriteRenderer;

    [SerializeField]
    private SerializedDictionary<ProfileState, Sprite> _profileTable;

    [SerializeField]
    private Camera _minimapCamera;

    private void Start()
    {
        ProfileImage.sprite = _profileTable[ProfileState.Normal];
        _tempHpRatio = 1.0f;
        BindMinimapRenderer();
        SyncMinimapImage();
    }

    private void OnEnable()
    {
        UIEvents.OnProfileChangeRequested += HandleProfileChangeRequested;
        UIEvents.OnMinimapImageChanged += HandleMinimapImageChanged;
        UIEvents.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;

        if (serverPlayer != null)
        {
            BindServerPlayer(serverPlayer);
        }
    }

    private void OnDisable()
    {
        UIEvents.OnProfileChangeRequested -= HandleProfileChangeRequested;
        UIEvents.OnMinimapImageChanged -= HandleMinimapImageChanged;
        UIEvents.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;

        if (_player != null)
        {
            _player.OnTakeDamage -= UpdateHpBar;
        }

        UnbindGameplay();
    }

    private void BindToPlayer(Player player)
    {
        if (_player != null)
        {
            _player.OnTakeDamage -= UpdateHpBar;
        }

        _player = player;
        if (_player == null)
        {
            return;
        }

        _combatManager = _player.GetCombatManager();
        _player.OnTakeDamage += UpdateHpBar;
        _skillButtonManager = GetComponentInChildren<SkillButtonManager>();
        if (_skillButtonManager != null)
        {
            _skillButtonManager.Init(_combatManager);
        }

        if (_combatManager != null)
        {
            _combatManager.BroadcastSkillCooldownStates();
        }
    }

    private void LateUpdate()
    {
        if (_minimapCamera != null && serverPlayer != null)
        {
            var p = serverPlayer.transform.position;
            _minimapCamera.transform.position = new Vector3(p.x, _minimapCamera.transform.position.y, p.z);
            return;
        }
        if (_minimapCamera == null || _player == null)
        {
            return;
        }

        var playerPos = _player.transform.position;
        var cameraPos = _minimapCamera.transform.position;

        _minimapCamera.transform.position = new Vector3(playerPos.x, cameraPos.y, playerPos.z);
    }

    private void BindMinimapRenderer()
    {
        if (_minimapSpriteRenderer != null)
        {
            return;
        }

        var minimapObject = GameObject.Find("minimaprect");
        if (minimapObject == null)
        {
            return;
        }

        _minimapSpriteRenderer = minimapObject.GetComponent<SpriteRenderer>();
    }

    private void SyncMinimapImage()
    {
        var container = EverWind.Gameplay.ContainerInMap.Current;
        var level = container != null && container.WorldCatalog != null ? container.WorldCatalog.Get(container.MapId) : null;

        if (level != null)
        {
            HandleMinimapImageChanged(level.Minimap, level.MinimapPosition, level.MinimapRotation, level.MinimapScale);
        }
    }

    private void UpdateHpBar(float hp)
    {
        float ratio = hp / _player.GetPlayerStatManager().GetMaxHp();
        _tempHpRatio = ratio;
        HpBar.fillAmount = _tempHpRatio;
    }

    public void ChangeProfile(ProfileState state, float time)
    {
        if (profileRoutine != null)
        {
            StopCoroutine(profileRoutine);
        }

        profileRoutine = StartCoroutine(ChangeProfileCoroutine(state, time));
    }

    private void HandleProfileChangeRequested(ProfileState state, float duration)
    {
        ChangeProfile(state, duration);
    }

    private void HandleMinimapImageChanged(Sprite minimapSprite, Vector3 minimapPosition, Vector3 minimapRotation, float minimapScale)
    {
        BindMinimapRenderer();
        if (_minimapSpriteRenderer == null)
        {
            return;
        }

        _minimapSpriteRenderer.sprite = minimapSprite;
        _minimapSpriteRenderer.transform.localPosition = minimapPosition;
        _minimapSpriteRenderer.transform.localEulerAngles = minimapRotation;
        _minimapSpriteRenderer.transform.localScale = new Vector3(minimapScale, minimapScale, minimapScale);
    }

    private void HandleLocalPlayerSpawned(Player player)
    {
        BindToPlayer(player);
    }

    private IEnumerator ChangeProfileCoroutine(ProfileState state, float time)
    {
        ProfileImage.sprite = _profileTable[state];

        yield return new WaitForSeconds(time);

        if (_tempHpRatio <= 0.3f)
        {
            ProfileImage.sprite = _profileTable[ProfileState.Hurt];
        }
        else
            ProfileImage.sprite = _profileTable[ProfileState.Normal];

        profileRoutine = null;
    }

    public void OnQuitButton()
    {
        if (NetworkClient.Current != null)
        {
            NetworkClient.Current.Logout();
            return;
        }
        var networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        if (!networkClient)
        {
            return;
        }
        networkClient.Logout();
    }

    public void BindServerPlayer(EverWind.Gameplay.PlayerServerTransmitter player)
    {
        UnbindGameplay();
        gameplayEvents = EverWind.Gameplay.GameplayIntegration.Active;

        if (gameplayEvents != null)
        {
            gameplayEvents.HealthChanged += HandleGameplayHealth;
            gameplayEvents.Feedback += HandleGameplayFeedback;
        }

        serverPlayer = player;
        _skillButtonManager = GetComponentInChildren<SkillButtonManager>(true);
        _skillButtonManager?.Init(null);
        if (player != null)
        {
            ApplyServerStats(player.Hp, player.MaxHp);
        }
    }

    private void UnbindGameplay()
    {
        if (gameplayEvents != null)
        {
            gameplayEvents.HealthChanged -= HandleGameplayHealth;
            gameplayEvents.Feedback -= HandleGameplayFeedback;
            gameplayEvents = null;
        }
    }

    private void HandleGameplayHealth(EverWind.Gameplay.HealthChanged change)
    {
        if (serverPlayer != null && change.Entity == serverPlayer.EntityId)
        {
            ApplyServerStats(change.Hp, change.MaxHp);
        }
    }

    private void HandleGameplayFeedback(EverWind.Gameplay.GameplayFeedback feedback)
    {
        ChangeProfile(feedback == EverWind.Gameplay.GameplayFeedback.Hit ? ProfileState.Hit : ProfileState.Success, feedback == EverWind.Gameplay.GameplayFeedback.Hit ? .4f : 1f);
    }

    public void ApplyServerStats(float hp, float maxHp)
    {
        _tempHpRatio = maxHp > 0 ? Mathf.Clamp01(hp / maxHp) : 0;
        HpBar.fillAmount = _tempHpRatio;
    }
}
