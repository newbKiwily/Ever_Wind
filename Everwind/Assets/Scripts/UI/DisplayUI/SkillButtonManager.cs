using UnityEngine;

public class SkillButtonManager : MonoBehaviour
{
    [SerializeField]
    private SkillButton[] _skillButtons; // 인스펙터에서 5개의 SkillButton을 할당

    public void Init(CombatManager manager)
    {
        if (_skillButtons == null || _skillButtons.Length == 0)
            _skillButtons = GetComponentsInChildren<SkillButton>();

        for (int i = 0; i < _skillButtons.Length; i++)
        {
            if (_skillButtons[i] != null)
            {
                _skillButtons[i].SetIndex(i); 
                _skillButtons[i].Init(manager);
            }
        }
    }
}