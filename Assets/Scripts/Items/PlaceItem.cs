using UnityEngine;

[CreateAssetMenu]
public class PlaceItem : ConsumeItem
{
    public GameObject Placement;

    public override void Use(System.Action<InventoryItem> requestUse)
    {
        requestUse?.Invoke(this);
    }
}
