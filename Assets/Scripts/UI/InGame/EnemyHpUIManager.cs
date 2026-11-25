using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class EnemyHpUIManager : MonoBehaviour
{
    public Canvas Canvas;
    public GameObject HpPrefab, DamagePrefab;
    readonly Dictionary<int, GameObject> rows = new Dictionary<int, GameObject>();
    void OnEnable() { PlayEvents.UIStateChanged += Apply; PlayEvents.UIDamageShown += Damage; }
    void OnDisable()
    {
        PlayEvents.UIStateChanged -= Apply; PlayEvents.UIDamageShown -= Damage;
        foreach (var row in rows.Values) if (row != null) Destroy(row);
        rows.Clear();
    }
    void Position(GameObject go, Vector3 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Canvas.transform, screen, Canvas.worldCamera, out var position);
        ((RectTransform)go.transform).anchoredPosition = position;
    }
    void Apply(InGameUIState state)
    {
        if (!state.Ready)
        {
            foreach (var row in rows.Values) if (row != null) Destroy(row);
            rows.Clear(); return;
        }
        var seen = new HashSet<int>();
        foreach (var enemy in state.Enemies)
        {
            seen.Add(enemy.Id);
            if (!rows.TryGetValue(enemy.Id, out var row) || row == null) { row = Instantiate(HpPrefab, transform); rows[enemy.Id] = row; }
            row.SetActive(enemy.ScreenPosition.z > 0); Position(row, enemy.ScreenPosition);
            var fill = row.transform.Find("Fill"); if (fill != null) fill.GetComponent<Image>().fillAmount = enemy.MaxHp > 0 ? enemy.Hp / enemy.MaxHp : 0;
        }
        foreach (var row in rows) if (!seen.Contains(row.Key) && row.Value != null) row.Value.SetActive(false);
    }
    void Damage(UIDamageData data)
    {
        if (data.ScreenPosition.z <= 0 || DamagePrefab == null) return;
        var go = Instantiate(DamagePrefab, transform); Position(go, data.ScreenPosition);
        go.GetComponent<TMP_Text>().text = data.Amount.ToString(); Destroy(go, 1);
    }
}
