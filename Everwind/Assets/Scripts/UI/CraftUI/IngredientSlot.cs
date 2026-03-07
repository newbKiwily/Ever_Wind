using TMPro;
using UnityEngine.UI;
using UnityEngine;
public class IngredientSlot : Slot
{

    private TextMeshProUGUI _amountText;

    protected override void Awake()
    {
        _amountText = transform.Find("AmountText").GetComponent<TextMeshProUGUI>();  
    }

    public void Initialize(InventoryItem ingredientItem,int amount)
    {
        _item=ingredientItem;
        this.GetComponent<Image>().sprite = ingredientItem.ItemImage;   

        int owned = SingletonManager.Instance.GetSingleton<PopUpUIManager>().Inventory.GetItemCount(_item);

        _amountText.color = (owned >= amount) ? Color.green : Color.red;
        _amountText.text = $"{owned} / {amount}";
    }

}
