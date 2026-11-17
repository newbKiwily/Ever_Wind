using AYellowpaper.SerializedCollections;
using System.Collections;
using UnityEngine;

public class ItemMediator : SingletonBase<ItemMediator>
{
    public override bool IsPersistent => false;

    public SerializedDictionary<string, InventoryItem> ItemTable = new SerializedDictionary<string, InventoryItem>();
    public SerializedDictionary<int, GameObject> FieldItemTable = new SerializedDictionary<int, GameObject>();

    protected override void Awake()
    {
        Priority = 30;
        base.Awake();
    }

    public InventoryItem GetItemInfo(string itemName)
    {
        var item = ItemTable[itemName];
        if (item == null)
            return null;
        return item;
    }

}
