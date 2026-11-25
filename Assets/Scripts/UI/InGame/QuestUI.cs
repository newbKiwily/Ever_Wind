using System.Collections.Generic;
public sealed class QuestUI : UIListPanel
{
    protected override void Apply(InGameUIState state)
    {
        var data = new List<UISlotData>();
        foreach (var quest in state.Quests) data.Add(new UISlotData { Kind = UISlotKind.Quest, Key = quest.Id, Name = quest.Name, Description = quest.Description, Interactable = state.Ready && quest.Complete && !quest.Claimed });
        Render(data);
    }
}
