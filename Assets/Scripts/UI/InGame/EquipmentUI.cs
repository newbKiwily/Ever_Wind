using System.Collections.Generic;
public sealed class EquipmentUI : UIListPanel
{
    protected override void Apply(InGameUIState state)
    {
        var data = new List<UISlotData>();
        string[] names = { "투구", "장갑", "신발", "무기", "갑옷" };
        int[] order = { 0, 4, 3, 1, 2 }; // main scene: head, body, weapon, gloves, boots.
        foreach (int i in order)
        {
            var row = new UISlotData { Kind = UISlotKind.Equipment, Key = 1000 + i, Name = names[i], Description = "장착된 아이템이 없습니다.", Count = names[i] };
            foreach (var item in state.Equipment) if (item.Slot == row.Key) { row.CommandId = item.Id; row.Name = item.Name; row.Description = item.Description; row.Icon = item.Icon; row.Interactable = state.Ready && !state.Dead; break; }
            data.Add(row);
        }
        Render(data);
    }
}
