using System.Drawing;
using UnityEngine.UI;

public class EquipmentSlot : ButtonSlot
{
    private Image _itemImage;
    public bool IsEquipped;
    private EquipmentUI _equipmentUI;

    protected override void Awake()
    {
        base.Awake();
        _itemImage = GetComponent<Image>();
    }

    public void Initialize(EquipmentItem newItem, EquipmentUI equipmentUI)
    {
        this._equipmentUI = equipmentUI;
        this._item = newItem;
        this.IsEquipped = true;
        _itemImage.color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 1.0f);
        if (_itemImage != null && newItem != null)
        {
            _itemImage.sprite = newItem.ItemImage;
          
        }
    }

    public void Clear()
    {
        this._item = null;
        this.IsEquipped = false;
        _itemImage.color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 0.0f);
        if (_itemImage != null)
        {
            _itemImage.sprite = null;
        }

    }

    protected override void OnClick()
    {
        if (_item == null) return;
        _equipmentUI.Unequip(this);
    }
}