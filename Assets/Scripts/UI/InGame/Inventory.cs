using System.Collections.Generic;
public sealed class Inventory : UIListPanel
{
    protected override void Apply(InGameUIState state)
    {
        var data = new List<UISlotData>();
        foreach (var item in state.Items) data.Add(new UISlotData { Kind = UISlotKind.Inventory, Key = item.Id, CommandId = item.Id, Name = item.Name, Description = item.Description, Icon = item.Icon, Count = item.Count.ToString(), Interactable = state.Ready && !state.Dead && item.Usable });
        Render(data);
    }
}
