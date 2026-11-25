using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class DisplayUIManager : MonoBehaviour
{
    public TMP_Text Health, Growth, Target;
    public Image HpBar;
    public RectTransform MinimapPlayer;
    void OnEnable() { PlayEvents.UIStateChanged += Apply; PlayEvents.EvUIStateRequested(); }
    void OnDisable() { PlayEvents.UIStateChanged -= Apply; }
    void Apply(InGameUIState state)
    {
        Health.text = state.Ready ? Mathf.CeilToInt(state.Hp) + " / " + Mathf.CeilToInt(state.MaxHp) : "접속 준비 중";
        HpBar.fillAmount = state.MaxHp > 0 ? Mathf.Clamp01(state.Hp / state.MaxHp) : 0;
        Growth.text = "Lv. " + state.Level + "    EXP " + state.Experience + " / " + state.RequiredExperience;
        Target.text = "";
        foreach (var enemy in state.Enemies) if (enemy.Id == state.SelectedTarget) Target.text = enemy.Name + "    " + Mathf.CeilToInt(enemy.Hp) + " / " + Mathf.CeilToInt(enemy.MaxHp);
    }
}
