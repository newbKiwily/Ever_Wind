using UnityEngine;

public class InputManager : SingletonBase<InputManager>
{
    [SerializeField]
    private TutorialGuide _tutorialGuide;

    private bool _isRestricted = false;

    protected override void Awake()
    {
        Priority = 1;
        base.Awake();
    }

    public override void Init()
    {
        if (_tutorialGuide == null)
        {
            _tutorialGuide = GameObject.Find("TutorialManager").GetComponent<TutorialGuide>();
        }
    }

    public bool GetInventoryKeyDown()
    {
        if (_isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.I);
    }

    public bool GetCraftUIKeyDown()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move ||
            _tutorialGuide.GetTutorialStepType() == TutorialStep.Combat || _tutorialGuide.GetTutorialStepType() == TutorialStep.Interact || _isRestricted)
            return false;

        return Input.GetKeyDown(KeyCode.C);
    }

    public bool GetUICloseKeyDown()
    {
        if (_isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public bool GetExitGameKeyDown()
    {
        if (!SingletonManager.Instance.GetSingleton<PopUpUIManager>().IsPopUpOn)
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        return GetUICloseKeyDown();
    }

    // 이동 입력
    public float GetHorizontal()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _isRestricted)
            return 0;

        return Input.GetAxis("Horizontal");
    }

    public float GetVertical()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _isRestricted)
            return 0;
        return Input.GetAxis("Vertical");
    }

    // 점프 입력 (상호작용으로 사용 중)
    public bool GetInteractDown()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move ||
            _tutorialGuide.GetTutorialStepType() == TutorialStep.Combat || _isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.Space);
    }

    // 공격 입력
    public bool GetEnterCombatDown()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move || _isRestricted)
            return false;
        return Input.GetMouseButtonDown(0); // 왼쪽 클릭
    }

    public bool GetControlCamera()
    {
        return Input.GetMouseButton(1); // 오른쪽 클릭 (코드상 1)
    }

    public bool GetChangeTargetDown()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move || _isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.E);
    }

    // "아무 키 입력" (마우스 제외)
    public bool AnyKeyDownExcludeMouse()
    {
        if (Input.anyKeyDown)
        {
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
                return true;
        }
        return false;
    }

    public int GetAttackKeyDown()
    {
        if (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move || _isRestricted)
            return 0;
        for (int i = 1; i <= 5; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKeyDown(key))
                return i; // 1~5 반환
        }
        return 0; // 입력 없음
    }

    public void LockMoveAndAttack()
    {
        _isRestricted = true;
    }

    public void UnlockMoveAndAttack()
    {
        _isRestricted = false;
    }

    public bool IsLocked()
    {
        return _isRestricted;
    }
}