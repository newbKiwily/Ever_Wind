using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField]
    private int _skillIndex;
    [SerializeField]
    private Image _cooldownImage;
    [SerializeField]
    private Button _button;

    private CombatManager _combatManager;
    private EverWind.Gameplay.IGameplayEventSource gameplayEvents;
    private EverWind.Gameplay.SkillCooldownChanged cooldown;

    private void OnEnable()
    {
        UIEvents.OnSkillCooldownChanged += HandleSkillCooldownChanged;
        BindGameplay();
    }

    private void OnDisable()
    {
        UIEvents.OnSkillCooldownChanged -= HandleSkillCooldownChanged;

        if (gameplayEvents != null)
        {
            gameplayEvents.SkillCooldownChanged -= HandleGameplayCooldown;
            gameplayEvents = null;
        }
    }

    public void Init(CombatManager manager)
    {
        _combatManager = manager;
        if (_button == null)
        {
            _button = GetComponentInChildren<Button>();
        }
        _button.onClick.RemoveListener(OnButtonClick);
        _button.onClick.AddListener(OnButtonClick);
        BindGameplay();

        RefreshFromManager();
    }

    private void BindGameplay()
    {
        if (gameplayEvents != null)
        {
            gameplayEvents.SkillCooldownChanged -= HandleGameplayCooldown;
        }

        gameplayEvents = EverWind.Gameplay.GameplayIntegration.Active;

        if (gameplayEvents != null)
        {
            gameplayEvents.SkillCooldownChanged += HandleGameplayCooldown;
            cooldown = gameplayEvents.GetSkillCooldown(_skillIndex + 1);
        }
    }

    public void SetIndex(int index)
    {
        _skillIndex = index;
    }

    private void RefreshFromManager()
    {
        if (_combatManager == null)
        {
            return;
        }

        float ratio = _combatManager.GetSkillCooldownRatio(_skillIndex);
        bool isReady = _combatManager.IsSkillReady(_skillIndex);
        ApplyCooldownState(ratio, isReady);
    }

    private void HandleSkillCooldownChanged(int skillIndex, float ratio, bool isReady)
    {
        if (skillIndex != _skillIndex)
        {
            return;
        }

        ApplyCooldownState(ratio, isReady);
    }

    private void ApplyCooldownState(float ratio, bool isReady)
    {
        if (_cooldownImage != null)
        {
            _cooldownImage.fillAmount = ratio;
        }

        if (_button != null)
        {
            _button.interactable = isReady;
        }
    }

    private void HandleGameplayCooldown(EverWind.Gameplay.SkillCooldownChanged change)
    {
        if (change.Skill == _skillIndex + 1)
        {
            cooldown = change;
        }
    }

    private void Update()
    {
        if (gameplayEvents == null || NetworkClient.Current == null || !NetworkClient.Current.Ready || EverWind.Gameplay.GameplayIntegration.Active == null || EverWind.Gameplay.GameplayIntegration.Active.Catalog == null)
        {
            return;
        }

        double remaining = System.Math.Max(0, cooldown.EndsAt - NetworkClient.Current.EstimatedServerTime);

        bool available = EverWind.Gameplay.GameplayIntegration.Active.Catalog.HasSkill(_skillIndex + 1);

        ApplyCooldownState(cooldown.Duration > 0 ? Mathf.Clamp01(1 - (float)remaining / cooldown.Duration) : 1, available && remaining <= 0);
    }

    private void OnButtonClick()
    {
        if (EverWind.Gameplay.GameplayIntegration.Active != null)
        {
            EverWind.Gameplay.GameplayIntegration.Active.Attack(_skillIndex);
            return;
        }
        if (_combatManager != null)
        {
            _combatManager.ExecuteSkill(_skillIndex);
        }
    }
}
