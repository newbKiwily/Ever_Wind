
public abstract class ConsumeItem : InventoryItem, IUsableItem
{
    public abstract void Use(System.Action<InventoryItem> requestUse);

}
