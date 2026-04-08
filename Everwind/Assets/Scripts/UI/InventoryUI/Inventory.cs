using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Inventory : MonoBehaviour
{
    public GameObject SlotPrefab;
    public Transform LayoutParent;
    private List<InventorySlot> _slots = new List<InventorySlot>();

    public void Init()
    {
        var loadItems = SingletonManager.Instance.GetSingleton<DataCenter>().LoadItems;
        var itemMediator = SingletonManager.Instance.GetSingleton<ItemMediator>();

        loadItems.OrderBy(item => item.SlotIndex).ToList();

        foreach (var item in loadItems)
        {
            for (int i = 0; i < item.Amount; i++)
            {
                itemMediator.Mediation(item.Key);
            }
        }

        SingletonManager.Instance.GetSingleton<DataCenter>().LoadItems.Clear();
    }

    public void PutItem(InventoryItem item)
    {
        // 1. 중복 확인 (Stackable 아이템 처리)
        if (item.IsStackable)
        {
            foreach (var slot in _slots)
            {
                var itemInSlot = slot.GetInventoryItem();
                if (itemInSlot == item)
                {
                    slot.count++;
                    // 슬롯의 Initialize를 호출하여 데이터 전달 및 UI 갱신을 한 번에 처리
                    slot.Initialize(this, item, slot.count);
                    return;
                }
            }
        }

        // 2. 새 슬롯 생성 및 초기화
        GameObject obj = Instantiate(SlotPrefab, LayoutParent);
        InventorySlot newSlot = obj.GetComponent<InventorySlot>();

        if (newSlot != null)
        {
            // 모든 초기화 책임을 슬롯의 Initialize에 위임
            newSlot.Initialize(this, item, 1);
            _slots.Add(newSlot);
        }
    }

    public void UseItem(InventorySlot slot)
    {
        var itemInSlot = slot.GetInventoryItem();
        if (slot == null || itemInSlot == null) return;

        // 아이템 사용 인터페이스 확인
        if (itemInSlot is IUsableItem usable)
        {
            usable.Use();
            slot.count -= 1;
            UpdateInventory(); // 전체 상태 동기화
        }
    }

    public void UpdateInventory()
    {
        List<InventorySlot> removeList = new List<InventorySlot>();

        foreach (InventorySlot slot in _slots)
        {
            if (slot.count <= 0)
            {
                removeList.Add(slot);
                if (slot.gameObject != null) Destroy(slot.gameObject);
                continue;
            }

            // 남은 아이템들의 UI 갱신
            slot.Initialize(this, slot.GetInventoryItem(), slot.count);
        }

        foreach (var r in removeList)
        {
            _slots.Remove(r);
        }
    }

    public int GetItemCount(InventoryItem item)
    {
        foreach (InventorySlot slotItem in _slots)
        {
            if (slotItem.GetInventoryItem() == item) return slotItem.count;
        }
        return 0;
    }

    public void ConsumeItem(string itemName, int amount)
    {
        foreach (InventorySlot slotItem in _slots)
        {
            var itemInSlot = slotItem.GetInventoryItem();
            if (itemInSlot != null && itemInSlot.ItemName == itemName)
            {
                slotItem.count -= amount;
                break;
            }
        }
        UpdateInventory();
    }

    public List<DataCenter.InventoryData> GetCurrentInventoryData()
    {
        SingletonManager.Instance.GetSingleton<PopUpUIManager>().EquipmentUI.UnequipAll();

        List<DataCenter.InventoryData> inventoryDataList = new List<DataCenter.InventoryData>();

        for (int i = 0; i < _slots.Count; i++)
        {
            var itemInSlot = _slots[i].GetInventoryItem();
            if (itemInSlot == null) continue;

            DataCenter.InventoryData data = new DataCenter.InventoryData
            {
                Key = itemInSlot.ItemName,
                Amount = _slots[i].count,
                SlotIndex = i,
                IsEquppiedItem = itemInSlot.IsFromEquipmentUI
            };

            inventoryDataList.Add(data);
        }

        return inventoryDataList;
    }
}
