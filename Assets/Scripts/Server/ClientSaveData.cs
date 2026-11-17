using System.Collections.Generic;

public struct InventoryData
{
    public string Key;
    public int Amount, SlotIndex, IsEquppiedItem;
}

public struct QuestLoadData
{
    public int QuestId;
    public bool IsCompleted, RewardClaimed;
    public List<int> CurrentCounts;
}
