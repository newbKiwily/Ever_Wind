using UnityEngine;
using UnityEngine.UI;


public class SkillButton : MonoBehaviour
{
    [SerializeField] private int _skillIndex;
    [SerializeField] private Image _cooldownImage;
    [SerializeField] private Button _button;

    private CombatManager _combatManager;

    private void OnEnable()
    {
        UIEvents.OnSkillCooldownChanged += HandleSkillCooldownChanged;
    }

    private void OnDisable()
    {
        UIEvents.OnSkillCooldownChanged -= HandleSkillCooldownChanged;
    }

    public void Init(CombatManager manager)
    {
        _combatManager = manager;
        if (_button == null)
            _button = GetComponentInChildren<Button>();
        _button.onClick.AddListener(OnButtonClick);
        RefreshFromManager();
    }

    public void SetIndex(int index)
    {
        _skillIndex = index; 
    }

    private void RefreshFromManager()
    {
        if (_combatManager == null) return;

        float ratio = _combatManager.GetSkillCooldownRatio(_skillIndex);
        bool isReady = _combatManager.IsSkillReady(_skillIndex);
        ApplyCooldownState(ratio, isReady);
    }

    private void HandleSkillCooldownChanged(int skillIndex, float ratio, bool isReady)
    {
        if (skillIndex != _skillIndex)
            return;

        ApplyCooldownState(ratio, isReady);
    }

    private void ApplyCooldownState(float ratio, bool isReady)
    {
        if (_cooldownImage != null)
            _cooldownImage.fillAmount = ratio;

        if (_button != null)
            _button.interactable = isReady;
    }

    private void OnButtonClick()
    {
        if (_combatManager != null)
        {
            _combatManager.ExecuteSkill(_skillIndex);
        }
    }
}
