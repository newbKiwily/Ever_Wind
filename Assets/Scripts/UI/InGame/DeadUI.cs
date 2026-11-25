using TMPro;
using UnityEngine;
public sealed class DeadUI : MonoBehaviour
{
    public TMP_Text Remaining;
    void OnEnable() { PlayEvents.UIStateChanged += Apply; }
    void OnDisable() { PlayEvents.UIStateChanged -= Apply; }
    void Apply(InGameUIState state)
    {
        Remaining.text = state.ReviveRemaining > 0 ? "부활까지 " + Mathf.CeilToInt(state.ReviveRemaining) + "초" : "다시 일어설 준비가 되었습니다.";
        PlayEvents.EvUIControlState(new UIControlState { Control = "revive", Enabled = state.Ready && state.Dead && state.ReviveRemaining <= 0 });
    }
}
