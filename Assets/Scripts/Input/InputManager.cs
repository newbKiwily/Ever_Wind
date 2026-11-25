using System;
using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;

public class InputManager : SingletonBase<InputManager>
{
    public Func<int> ReadTutorialPhase;

    private bool _isRestricted = false;
    private bool _uiRestricted;
    private InGamePanel _openPanel;
    public int UIEscapeConsumedFrame { get; private set; } = -1;

    private void OnEnable() { PlayEvents.UIPanelChanged += ApplyUIPanel; }
    private void OnDisable() { PlayEvents.UIPanelChanged -= ApplyUIPanel; _uiRestricted = false; _openPanel = InGamePanel.None; }
    private void ApplyUIPanel(InGamePanel panel)
    {
        if (_openPanel != InGamePanel.None && panel == InGamePanel.None && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) UIEscapeConsumedFrame = Time.frameCount;
        _openPanel = panel;
        _uiRestricted = panel != InGamePanel.None && panel != InGamePanel.Quest;
    }

    public override bool IsPersistent => gameObject.scene.name != "InGame";

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
    }

    private int TutorialPhase => ReadTutorialPhase?.Invoke() ?? -1;

    public bool GetInventoryKeyDown()
    {
        if (_isRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.I);
    }

    public bool GetCraftUIKeyDown()
    {
        int phase = TutorialPhase;
        if ((phase >= 0 && phase <= 3) || _isRestricted)
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
        if (TutorialPhase == 0 || _isRestricted || _uiRestricted)
            return 0;
        return Input.GetAxis("Horizontal");
    }

    public float GetVertical()
    {
        if (TutorialPhase == 0 || _isRestricted || _uiRestricted)
            return 0;
        return Input.GetAxis("Vertical");
    }

    public bool GetInteractDown()
    {
        int phase = TutorialPhase;
        if ((phase >= 0 && phase <= 2) || _isRestricted || _uiRestricted)
            return false;
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool GetEnterCombatDown()
    {
        int phase = TutorialPhase;
        if ((phase >= 0 && phase <= 1) || _isRestricted || _uiRestricted)
            return false;
        return Input.GetMouseButtonDown(0);
    }

    public bool GetControlCamera()
    {
        return Input.GetMouseButton(1);
    }

    public bool GetChangeTargetDown()
    {
        int phase = TutorialPhase;
        if ((phase >= 0 && phase <= 1) || _isRestricted || _uiRestricted)
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
        int phase = TutorialPhase;
        if ((phase >= 0 && phase <= 1) || _isRestricted || _uiRestricted)
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
