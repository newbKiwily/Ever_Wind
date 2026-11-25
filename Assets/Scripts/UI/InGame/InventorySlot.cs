public sealed class InventorySlot : ButtonSlot { protected override void OnClick(){PlayEvents.EvUICommand(UICommandKind.UseItem,Value);} }
