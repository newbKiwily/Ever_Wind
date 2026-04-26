using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Inventory : MonoBehaviour
{
    public GameObject SlotPrefab;
    public Transform LayoutParent;
    private List<InventorySlot> _slots = new List<InventorySlot>();
    private ScrollRect _scrollRect;
    private RectTransform _scrollArea;
    private RectTransform _contentRect;
    private Camera _eventCamera;
    private const float ScrollSensitivity = 0.08f;

    public void Init()
    {
        _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (_scrollRect != null)
        {
            _scrollArea = _scrollRect.GetComponent<RectTransform>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.horizontalScrollbar = null;
            _scrollRect.scrollSensitivity = 0f;

            if (LayoutParent is RectTransform contentRect)
            {
                _contentRect = contentRect;
                _scrollRect.content = contentRect;
            }
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _eventCamera = null;
        }
        else
        {
            _eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

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
        RefreshContentHeight();
    }

    private void Update()
    {
        if (_scrollRect == null || _scrollArea == null)
            return;

        float wheelDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(wheelDelta, 0f))
            return;

        bool isPointerOverScrollArea = RectTransformUtility.RectangleContainsScreenPoint(_scrollArea, Input.mousePosition, _eventCamera);
        if (!isPointerOverScrollArea)
            return;

        float nextPosition = _scrollRect.verticalNormalizedPosition + wheelDelta * ScrollSensitivity;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(nextPosition);
    }

    public void PutItem(InventoryItem item)
    {
        if (item.IsStackable)
        {
            foreach (var slot in _slots)
            {
                var itemInSlot = slot.GetInventoryItem();
                if (itemInSlot == item)
                {
                    slot.count++;
                    slot.Initialize(this, item, slot.count);
                    RefreshContentHeight();
                    return;
                }
            }
        }

        GameObject obj = Instantiate(SlotPrefab, LayoutParent);
        InventorySlot newSlot = obj.GetComponent<InventorySlot>();

        if (newSlot != null)
        {
            newSlot.Initialize(this, item, 1);
            _slots.Add(newSlot);
            RefreshContentHeight();
        }
    }

    public void UseItem(InventorySlot slot)
    {
        var itemInSlot = slot.GetInventoryItem();
        if (slot == null || itemInSlot == null) return;

        if (itemInSlot is IUsableItem usable)
        {
            usable.Use();
            slot.count -= 1;
            UpdateInventory();
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

            slot.Initialize(this, slot.GetInventoryItem(), slot.count);
        }

        foreach (var r in removeList)
        {
            _slots.Remove(r);
        }

        RefreshContentHeight();
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

    private void RefreshContentHeight()
    {
        if (_contentRect == null)
            return;

        GridLayoutGroup gridLayout = _contentRect.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
            return;

        int itemCount = _slots.Count;
        int columnCount = GetColumnCount(gridLayout, _contentRect.rect.width);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)columnCount));

        float height =
            gridLayout.padding.top +
            gridLayout.padding.bottom +
            (rowCount * gridLayout.cellSize.y) +
            (Mathf.Max(0, rowCount - 1) * gridLayout.spacing.y);

        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private static int GetColumnCount(GridLayoutGroup gridLayout, float width)
    {
        if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, gridLayout.constraintCount);

        float usableWidth = width - gridLayout.padding.left - gridLayout.padding.right + gridLayout.spacing.x;
        float cellWidth = gridLayout.cellSize.x + gridLayout.spacing.x;
        return Mathf.Max(1, Mathf.FloorToInt(usableWidth / Mathf.Max(1f, cellWidth)));
    }
}
