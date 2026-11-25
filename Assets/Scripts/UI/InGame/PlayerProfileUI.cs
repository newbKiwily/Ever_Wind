using UnityEngine;

public sealed class PlayerProfileUI : MonoBehaviour
{
    public UnityEngine.UI.Image Portrait;
    public Sprite Normal, Hit, Success, Hurt;
    UIProfileState transient;
    float until;
    bool lowHealth;
    void OnEnable()
    {
        until = 0; lowHealth = false;
        PlayEvents.UIStateChanged += Apply;
        PlayEvents.UIProfileChanged += Change;
        Refresh(); PlayEvents.EvUIStateRequested();
    }
    void OnDisable() { PlayEvents.UIStateChanged -= Apply; PlayEvents.UIProfileChanged -= Change; }
    void Apply(InGameUIState state)
    {
        lowHealth = state.Ready && (state.Dead || state.MaxHp > 0 && state.Hp / state.MaxHp <= .3f);
        if (!state.Ready) until = 0;
        Refresh();
    }
    void Change(UIProfileState state, float duration)
    {
        if (transient == UIProfileState.Hit && Time.unscaledTime < until && state == UIProfileState.Success) return;
        transient = state; until = Time.unscaledTime + Mathf.Max(0, duration); Refresh();
    }
    void Update() { if (until > 0 && Time.unscaledTime >= until) { until = 0; Refresh(); } }
    void Refresh()
    {
        if (Portrait == null) return;
        var state = Time.unscaledTime < until ? transient : lowHealth ? UIProfileState.Hurt : UIProfileState.Normal;
        var sprite = state == UIProfileState.Hit ? Hit : state == UIProfileState.Success ? Success : state == UIProfileState.Hurt ? Hurt : Normal;
        Portrait.sprite = sprite != null ? sprite : Normal;
        Portrait.enabled = Portrait.sprite != null;
    }
}
