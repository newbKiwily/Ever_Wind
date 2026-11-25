using System;
using TMPro;
using UnityEngine;

public sealed class InGameUIController : MonoBehaviour
{
    [Serializable] public struct PanelEntry { public InGamePanel Kind; public GameObject Root; }
    public PanelEntry[] Panels;
    public TMP_Text Notice;
    public GameObject Tooltip;
    public TMP_Text TooltipTitle, TooltipDescription;
    public Canvas Canvas;
    InGamePanel current;
    bool dead;
    bool ready;
    float noticeUntil;
    void OnEnable()
    {
        PlayEvents.UIPanelRequested += Toggle;
        PlayEvents.UIStateChanged += State;
        PlayEvents.UITooltipChanged += ShowTooltip;
        PlayEvents.UINotice += ShowNotice;
        SetPanel(InGamePanel.None);
        PlayEvents.EvUIStateRequested();
    }
    void OnDisable()
    {
        PlayEvents.UIPanelRequested -= Toggle; PlayEvents.UIStateChanged -= State;
        PlayEvents.UITooltipChanged -= ShowTooltip; PlayEvents.UINotice -= ShowNotice;
        PlayEvents.EvUIPanelChanged(InGamePanel.None);
    }
    void Toggle(InGamePanel panel)
    {
        if (dead && panel != InGamePanel.Dead) return;
        SetPanel(current == panel ? InGamePanel.None : panel);
        PlayEvents.EvUIStateRequested();
    }
    void SetPanel(InGamePanel panel)
    {
        current = panel;
        foreach (var entry in Panels) if (entry.Root != null && entry.Root.activeSelf != (entry.Kind == panel)) entry.Root.SetActive(entry.Kind == panel);
        if (Tooltip != null) Tooltip.SetActive(false);
        PlayEvents.EvUIPanelChanged(panel);
    }
    void State(InGameUIState state)
    {
        bool reset = ready && !state.Ready;
        ready = state.Ready;
        if (dead != state.Dead) { dead = state.Dead; SetPanel(dead ? InGamePanel.Dead : InGamePanel.None); }
        else if (reset) SetPanel(InGamePanel.None);
    }
    void ShowNotice(string text) { Notice.text = text; noticeUntil = Time.unscaledTime + 3; }
    void Update() { if (Notice != null && Time.unscaledTime > noticeUntil) Notice.text = ""; }
    void ShowTooltip(UITooltipData data)
    {
        if (Tooltip == null) return;
        Tooltip.SetActive(data.Visible);
        if (!data.Visible) return;
        TooltipTitle.text = data.Title; TooltipDescription.text = data.Description;
        var rect = (RectTransform)Tooltip.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Canvas.transform, data.ScreenPosition, Canvas.worldCamera, out var point);
        var bounds = ((RectTransform)Canvas.transform).rect;
        rect.anchoredPosition = new Vector2(Mathf.Clamp(point.x + 18, bounds.xMin, bounds.xMax - rect.rect.width), Mathf.Clamp(point.y - 18, bounds.yMin + rect.rect.height, bounds.yMax));
    }
}
