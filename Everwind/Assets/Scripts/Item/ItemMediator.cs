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

    public void Mediation(string key)
    {
        var target = ItemTable[key];
    
        SingletonManager.Instance.GetSingleton<PopUpUIManager>().Inventory.PutItem(target);
       
    }

    public void ItemRespawn(GameObject itemObject, float delay)
    {
        itemObject.SetActive(false);
        StartCoroutine(RespawnAfterDelay(itemObject, delay));
    }

    private IEnumerator RespawnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);

        FieldItem fieldItem = obj.GetComponent<FieldItem>();
        if (fieldItem != null)
        {
            fieldItem.Initialize();
        }
    }

    public InventoryItem GetItemInfo(string itemName)
    {
        var item = ItemTable[itemName];
        if (item == null)
            return null;
        return item;
    }

    public int GetFieldItemId(GameObject fieldItemObject)
    {
        if (fieldItemObject == null)
            return 0;

        foreach (var pair in FieldItemTable)
        {
            GameObject registeredObject = pair.Value;
            if (registeredObject == null)
                continue;

            if (registeredObject == fieldItemObject)
                return pair.Key;

            if (registeredObject.name == fieldItemObject.name.Replace("(Clone)", "").Trim())
                return pair.Key;

            FieldItem registeredFieldItem = registeredObject.GetComponent<FieldItem>();
            FieldItem targetFieldItem = fieldItemObject.GetComponent<FieldItem>();

            if (registeredFieldItem != null &&
                targetFieldItem != null &&
                registeredFieldItem.GetType() == targetFieldItem.GetType())
            {
                return pair.Key;
            }
        }

        return 0;
    }
}

