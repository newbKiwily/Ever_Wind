using UnityEngine;
using UnityEngine.UI;


public class SkillButton : MonoBehaviour
{
    [SerializeField] private int _skillIndex;
    [SerializeField] private Image _cooldownImage;
    [SerializeField] private Button _button;

    private CombatManager _combatManager;

    public void Init(CombatManager manager)
    {
        _combatManager = manager;
        if (_button == null)
            _button = GetComponentInChildren<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }

    public void SetIndex(int index)
    {
        _skillIndex = index; 
    }

    private void Update()
    {
        if (_combatManager == null) return;

        float ratio = _combatManager.GetSkillCooldownRatio(_skillIndex);
        if (_cooldownImage != null)
            _cooldownImage.fillAmount = ratio;

        bool isReady = _combatManager.IsSkillReady(_skillIndex);
        _button.interactable = isReady;
    }

    private void OnButtonClick()
    {
        if (_combatManager != null)
        {
            _combatManager.ExecuteSkill(_skillIndex - 1);
        }
    }
}