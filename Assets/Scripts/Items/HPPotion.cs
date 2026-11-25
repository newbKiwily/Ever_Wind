using UnityEngine;

[CreateAssetMenu]
public class HPPotion : ConsumeItem
{
    public float HealAmount;

    public override void Use(System.Action<InventoryItem> requestUse)
    {
        requestUse?.Invoke(this);
    }
}
