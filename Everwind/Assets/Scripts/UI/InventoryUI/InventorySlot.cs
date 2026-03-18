using TMPro;
using UnityEngine.UI;

public class InventorySlot : ButtonSlot
{
    private TextMeshProUGUI _countText;
    private Image _itemImage;
    public int count;
    private Inventory _inventory;

    protected override void Awake()
    {
        base.Awake();
        _countText = transform.Find("Count").GetComponent<TextMeshProUGUI>();
        _itemImage = transform.Find("ItemSlot").GetComponent<Image>();
    }


    public void Initialize(Inventory inv, InventoryItem newItem, int amount)
    {
        this._inventory = inv;
        this._item = newItem; // Slot.csÀÇ protected item »ç¿ë
        this.count = amount;

        if (_itemImage != null && _item != null) _itemImage.sprite = _item.ItemImage;
        if (_countText != null) _countText.text = count > 1 ? count.ToString() : "";
    }

    protected override void OnClick()
    {
        if (_item != null)
            _inventory.UseItem(this);
    }
}