using System.Collections.Generic;
using UnityEngine;

public abstract class UIListPanel : MonoBehaviour
{
    public Slot SlotPrefab;
    public Transform Content;
    protected readonly Dictionary<int, Slot> rows = new Dictionary<int, Slot>();
    protected virtual void OnEnable() { PlayEvents.UIStateChanged += Apply; PlayEvents.EvUIStateRequested(); }
    protected virtual void OnDisable() { PlayEvents.UIStateChanged -= Apply; }
    protected abstract void Apply(InGameUIState state);
    protected void Render(IEnumerable<UISlotData> data) { RenderInto(data, rows, Content, SlotPrefab); }
    protected static void RenderInto(IEnumerable<UISlotData> data, Dictionary<int, Slot> views, Transform parent, Slot prefab)
    {
        var visible = new HashSet<int>();
        foreach (var row in data)
        {
            visible.Add(row.Key);
            if (!views.TryGetValue(row.Key, out var view) || view == null)
            {
                view = Instantiate(prefab, parent); view.Kind = row.Kind; view.Key = row.Key;
                view.ControlId = row.Kind + ":" + row.Key; views[row.Key] = view;
            }
            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
            PlayEvents.EvUISlot(row);
        }
        foreach (var view in views) if (view.Value != null && !visible.Contains(view.Key)) view.Value.gameObject.SetActive(false);
    }
}
