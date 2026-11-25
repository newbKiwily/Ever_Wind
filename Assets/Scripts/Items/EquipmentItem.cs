
using UnityEngine;

[CreateAssetMenu]
public class EquipmentItem : InventoryItem, IUsableItem
{

    public enum EquipmentType
    {
        Helmet = 1000,
        Gloves = 1001,
        Boots = 1002,
        Weapon = 1003,
        FullBody = 1004
    }

    public EquipmentType Type;
    public int Power;

    public void Use(System.Action<InventoryItem> requestUse)
    {
        requestUse?.Invoke(this);
    }

}
