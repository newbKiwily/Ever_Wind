using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;

public class InputManager : SingletonBase<InputManager>
{
    [SerializeField]
    private TutorialGuide _tutorialGuide;

    private bool _isRestricted = false;

    public override bool IsPersistent => EverWind.Gameplay.GameplayIntegration.Active == null;

    public static InputManager Current => Instance;

    public static bool IsTyping
    {
        get {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            return selected != null &&
                   ((selected.GetComponent<InputField>() is InputField field && field.isFocused) || (selected.GetComponent<TMPro.TMP_InputField>() is TMPro.TMP_InputField tmp && tmp.isFocused));
        }
    }

    public static bool IsPointerOverUI =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    protected override void Awake()
    {
        Priority = 10;

        base.Awake();
    }

    public override void Init()
    {
        if (_tutorialGuide == null)
        {
            _tutorialGuide = GameObject.Find("TutorialManager").GetComponent<TutorialGuide>();
        }
    }

    private bool HasTutorialRestriction()
    {
        return _tutorialGuide != null && _tutorialGuide.HasActiveTutorial();
    }

    public bool GetInventoryKeyDown()
    {
        if (_isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.I);
    }

    public bool GetCraftUIKeyDown()
    {
        if (HasTutorialRestriction() && (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move || _tutorialGuide.GetTutorialStepType() == TutorialStep.Combat || _tutorialGuide.GetTutorialStepType() == TutorialStep.Interact) || _isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.C);
    }

    public bool GetQuestUIKeyDown()
    {
        return Input.GetKeyDown(KeyCode.Q);
    }

    public bool GetUICloseKeyDown()
    {
        if (_isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public bool GetExitGameKeyDown()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public float GetHorizontal()
    {
        if ((HasTutorialRestriction() && _tutorialGuide.GetTutorialStepType() == TutorialStep.Camera) || _isRestricted)
            return 0;
        return Input.GetAxis("Horizontal");
    }

    public float GetVertical()
    {
        if ((HasTutorialRestriction() && _tutorialGuide.GetTutorialStepType() == TutorialStep.Camera) || _isRestricted)
            return 0;
        return Input.GetAxis("Vertical");
    }

    public bool GetInteractDown()
    {
        if ((HasTutorialRestriction() && (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move || _tutorialGuide.GetTutorialStepType() == TutorialStep.Combat)) || _isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool GetEnterCombatDown()
    {
        if ((HasTutorialRestriction() && (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move)) || _isRestricted)
            return false;
        return Input.GetMouseButtonDown(0);
    }

    public bool GetControlCamera()
    {
        return Input.GetMouseButton(1);
    }

    public bool GetChangeTargetDown()
    {
        if ((HasTutorialRestriction() && (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move)) || _isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.E);
    }

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
        if ((HasTutorialRestriction() && (_tutorialGuide.GetTutorialStepType() == TutorialStep.Camera || _tutorialGuide.GetTutorialStepType() == TutorialStep.Move)) || _isRestricted)
            return 0;
        for (int i = 1; i <= 5; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + i);

            if (Input.GetKeyDown(key))
                return i;
        }

        return 0;
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

    public static Vector3 Movement(Transform camera, float horizontal, float vertical)
    {
        if (camera == null)
            return new Vector3(horizontal, 0, vertical);
        Vector3 forward = Vector3.Scale(camera.forward, new Vector3(1, 0, 1)).normalized;

        return forward * vertical + camera.right * horizontal;
    }

    public Vector3 ReadMovement()
    {
        return Movement(Camera.main != null ? Camera.main.transform : null, GetHorizontal(), GetVertical());
    }
}
