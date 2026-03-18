using AYellowpaper.SerializedCollections;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DisplayUIManager : SingletonBase<DisplayUIManager>
{
    public override bool IsPersistent => false;

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
    private SkillButtonManager _skillButtonManager;

    public Image HpBar;
    public Image ProfileImage;
    private float _tempHpRatio;

    [SerializeField]
    private SerializedDictionary<ProfileState, Sprite> _profileTable;

    [SerializeField]
    private Camera _minimapCamera;

    protected override void Awake()
    {
        Priority = 50;
        base.Awake();
    }

    private void OnEnable()
    {
        UIEvents.OnProfileChangeRequested += HandleProfileChangeRequested;
    }

    private void OnDisable()
    {
        UIEvents.OnProfileChangeRequested -= HandleProfileChangeRequested;
    }

    public override void Init()
    {
        ProfileImage.sprite = _profileTable[ProfileState.Normal];
        _tempHpRatio = 1.0f;
        BindToPlayer();
        _skillButtonManager = GetComponentInChildren<SkillButtonManager>();
        _skillButtonManager.Init(_combatManager);
    }

    private void BindToPlayer()
    {
        _player = SingletonManager.Instance.GetSingleton<WorldLoader>().InstancedPlayer.GetComponent<Player>();
        if (_player == null)
            return;

        _combatManager = _player.GetCombatManager();
        _player.OnTakeDamage += UpdateHpBar;
    }

    private void LateUpdate()
    {
        if (_minimapCamera == null || _player == null)
            return;

        var playerPos = _player.transform.position;
        var cameraPos = _minimapCamera.transform.position;

        _minimapCamera.transform.position = new Vector3(playerPos.x, cameraPos.y, playerPos.z);
    }

    private void UpdateHpBar(float hp)
    {
        float ratio = hp / _player.GetPlayerStatManager().GetMaxHp();
        _tempHpRatio = ratio;
        HpBar.fillAmount = _tempHpRatio;
    }

    public void ChangeProfile(ProfileState state, float time)
    {
        StartCoroutine(ChangeProfileCoroutine(state, time));
    }

    private void HandleProfileChangeRequested(ProfileState state, float duration)
    {
        ChangeProfile(state, duration);
    }

    private IEnumerator ChangeProfileCoroutine(ProfileState state, float time)
    {
        ProfileImage.sprite = _profileTable[state];

        yield return new WaitForSeconds(time);

        if (_tempHpRatio <= 0.3f)
            ProfileImage.sprite = _profileTable[ProfileState.Hurt];
        else
            ProfileImage.sprite = _profileTable[ProfileState.Normal];
    }

    public void OnQuitButton()
    {
        var networkClient = SingletonManager.Instance.GetSingleton<NetworkClient>();
        if (!networkClient)
            return;
        networkClient.Logout();
    }
}
