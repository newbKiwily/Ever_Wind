using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EverWind.Gameplay;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class UIControlButton : MonoBehaviour
{
    public string ControlId;
    public bool OpensPanel;
    public InGamePanel Panel;
    public UICommandKind Command;
    public int Value;
    [Header("상호작용 효과 - 미지정 시 재생하지 않음")]
    public Effect InteractionEffect;
    public Vector2 EffectOffset;
    [Min(.001f)] public float EffectScale = 1;
    protected UnityEngine.UI.Button _button;
    protected virtual void Awake()
    {
        _button = GetComponent<UnityEngine.UI.Button>();
        _button.onClick.AddListener(Activate);
    }
    protected virtual void OnEnable()
    {
        PlayEvents.UIControlActivationRequested += OnActivation;
        PlayEvents.UIControlStateChanged += OnControlState;
    }
    protected virtual void OnDisable()
    {
        PlayEvents.UIControlActivationRequested -= OnActivation;
        PlayEvents.UIControlStateChanged -= OnControlState;
    }
    protected virtual void OnDestroy() { if (_button != null) _button.onClick.RemoveListener(Activate); }
    void OnActivation(string id) { if (!string.IsNullOrEmpty(ControlId) && id == ControlId) Activate(); }
    void OnControlState(UIControlState state)
    {
        if (state.Control != ControlId) return;
        Value = state.Value; _button.interactable = state.Enabled;
    }
    public void Activate()
    {
        if (!isActiveAndEnabled || _button == null || !_button.IsInteractable()) return;
        if (InteractionEffect != null)
        {
            var canvas = GetComponentInParent<Canvas>();
            var position = RectTransformUtility.WorldToScreenPoint(canvas != null ? canvas.worldCamera : null, ((RectTransform)transform).TransformPoint(((RectTransform)transform).rect.center));
            PlayEvents.EvUIEffect(new UIEffectRequest { Prefab = InteractionEffect, ScreenPosition = position + EffectOffset, Scale = EffectScale });
        }
        OnClick();
    }
    protected virtual void OnClick()
    {
        if (OpensPanel) PlayEvents.EvUIPanelRequested(Panel);
        else PlayEvents.EvUICommand(Command, Value);
    }
}
