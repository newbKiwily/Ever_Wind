using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftUI : MonoBehaviour
{
    private PopUpUIManager _popUpUIManager;
    private Inventory Inventory => _popUpUIManager.Inventory;
    private ItemMediator ItemMediator => SingletonManager.Instance.GetSingleton<ItemMediator>();

    public GameObject CraftListLayout;
    public GameObject CraftItemPrefab;

    public GameObject IngredientLayout;
    public GameObject IngredientPrefab;

    public static event Action CraftTClear;

    [SerializeField]
    private Image _craftItemImage;
    [SerializeField]
    private TextMeshProUGUI _craftItemName;

    [SerializeField]
    private List<CraftItemRecipe> _itemRecipes = new List<CraftItemRecipe>();

    [SerializeField]
    private Button _craftButton;

    private CraftItemRecipe _selectedRecipe;

    public void Init(PopUpUIManager popUpUIManager)
    {
        this._popUpUIManager = popUpUIManager;

        foreach (var recipe in _itemRecipes)
        {
            InstanceCraftListSlot(recipe);
        }

        _craftButton.onClick.AddListener(OnCraft);
        _craftButton.interactable = false;

        FlushCraftZone();
    }

    void InstanceCraftListSlot(CraftItemRecipe recipe)
    {
        var resultItem = ItemMediator.GetItemInfo(recipe.ResultItemName);
        if (resultItem == null)
            return;

        GameObject slot = Instantiate(CraftItemPrefab, CraftListLayout.transform);
        CraftSlot slotInfo = slot.GetComponent<CraftSlot>();

        slotInfo.Initialize(resultItem, recipe);
    }

    private void InstanceIngredientSlot(List<CraftItemRecipe.Ingredient> ingredients)
    {
        foreach (var ingredient in ingredients)
        {
            GameObject slot = Instantiate(IngredientPrefab, IngredientLayout.transform);
            IngredientSlot slotInfo = slot.GetComponent<IngredientSlot>();
            slotInfo.Initialize(ingredient.IngredientItem, ingredient.Amount);
        }
    }

    private bool CanCraft(CraftItemRecipe recipe)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            var item = ingredient.IngredientItem;
            int owned = Inventory.GetItemCount(item);

            if (owned < ingredient.Amount)
                return false;
        }

        return true;
    }

    public void UpdateCraftZone(CraftSlot slot)
    {
        FlushCraftZone();

        _selectedRecipe = slot.TargetRecipe;

        _craftItemImage.sprite = slot.SlotImage.sprite;
        _craftItemName.text = slot.ItemName.text;

        InstanceIngredientSlot(_selectedRecipe.Ingredients);
        _craftButton.interactable = CanCraft(_selectedRecipe);
    }

    public void FlushCraftZone()
    {
        _selectedRecipe = null;

        _craftItemImage.sprite = null;
        _craftItemName.text = "";
        _craftButton.interactable = false;

        foreach (Transform child in IngredientLayout.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnCraft()
    {
        if (_selectedRecipe == null)
            return;

        if (!CanCraft(_selectedRecipe))
        {
            Debug.Log("재료 부족으로 제작 실패.");
            return;
        }

        foreach (var ingredient in _selectedRecipe.Ingredients)
        {
            Inventory.ConsumeItem(ingredient.IngredientItem.ItemName, ingredient.Amount);
        }

        var resultItem = ItemMediator.GetItemInfo(_selectedRecipe.ResultItemName);
        if (resultItem != null)
        {
            Inventory.PutItem(resultItem);
            SingletonManager.Instance.GetSingleton<DisplayUIManager>().ChangeProfile(DisplayUIManager.ProfileState.Success, 1.0f);
        }

        FlushCraftZone();
        CraftTClear?.Invoke();
    }
}