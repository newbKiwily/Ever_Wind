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
        Debug.Log("인벤토리 아이템 생성됌");
        SingletonManager.Instance.GetSingleton<PopUpUIManager>().Inventory.PutItem(target);
        //인벤토리로 전송
    }

    // 아이템을 비활성화하고 일정 시간 뒤에 다시 활성화하는 메서드
    public void ItemRespawn(GameObject itemObject, float delay)
    {
        itemObject.SetActive(false);
        StartCoroutine(RespawnAfterDelay(itemObject, delay));
    }

    // 오브젝트를 일정 시간 후에 다시 활성화하는 코루틴
    private IEnumerator RespawnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);

        // FieldItem 컴포넌트를 가져와서 초기화
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
}
