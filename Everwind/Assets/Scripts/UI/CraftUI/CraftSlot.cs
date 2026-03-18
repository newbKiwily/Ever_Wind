using TMPro;
using UnityEngine.UI;

public class CraftSlot : ButtonSlot
{
    public Image SlotImage;
    public TextMeshProUGUI ItemName;
    private Button _selectButton;
    public CraftItemRecipe TargetRecipe;

    protected override void Awake()
    {
        SlotImage = transform.Find("ItemImagePanel/ItemImage").GetComponent<Image>();
        ItemName = transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
        _selectButton = transform.Find("SelectBotton").GetComponent<Button>();

        _selectButton.onClick.AddListener(OnClick);
    }

    protected override void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnClick);
    }

    protected override void OnClick()
    {
        if (TargetRecipe == null) return;

        CraftUI craftUI = GetComponentInParent<CraftUI>();
        if (craftUI == null) return;

        craftUI.UpdateCraftZone(this);
    }

    public void Initialize(InventoryItem targetItem, CraftItemRecipe targetRecipe)
    {
        this.SlotImage.sprite = targetItem.ItemImage;
        this.ItemName.text = targetItem.ItemName;
        this.TargetRecipe = targetRecipe;
        _item = targetItem;
    }
}