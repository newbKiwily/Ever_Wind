using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftUI : MonoBehaviour
{
    private PopUpUIManager _popUpUIManager;
    private Inventory Inventory => _popUpUIManager.Inventory;
    private ItemMediator ItemMediator => SingletonManager.Instance.GetSingleton<ItemMediator>();
    private ScrollRect _scrollRect;
    private RectTransform _scrollArea;
    private RectTransform _contentRect;
    private Camera _eventCamera;
    private const float ScrollSensitivity = 0.08f;

    public GameObject CraftListLayout;
    public GameObject CraftItemPrefab;

    public GameObject IngredientLayout;
    public GameObject IngredientPrefab;

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
        _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (_scrollRect != null)
        {
            _scrollArea = _scrollRect.GetComponent<RectTransform>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.horizontalScrollbar = null;
            _scrollRect.scrollSensitivity = 0f;

            if (CraftListLayout != null)
            {
                _contentRect = CraftListLayout.GetComponent<RectTransform>();
                _scrollRect.content = _contentRect;
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

        foreach (var recipe in _itemRecipes)
        {
            InstanceCraftListSlot(recipe);
        }

        _craftButton.onClick.AddListener(OnCraft);
        _craftButton.interactable = false;

        FlushCraftZone();
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

        _craftItemImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        _craftItemImage.sprite = slot.SlotImage.sprite;
        _craftItemName.text = slot.ItemName.text;

        InstanceIngredientSlot(_selectedRecipe.Ingredients);
        _craftButton.interactable = CanCraft(_selectedRecipe);
    }

    public void FlushCraftZone()
    {
        _selectedRecipe = null;

        _craftItemImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
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
            Debug.Log("Not enough ingredients to craft the selected recipe.");
            return;
        }

        foreach (var ingredient in _selectedRecipe.Ingredients)
        {
            Inventory.ConsumeItem(ingredient.IngredientItem.ItemName, ingredient.Amount);
        }

        string craftedItemName = _selectedRecipe.ResultItemName;
        var resultItem = ItemMediator.GetItemInfo(craftedItemName);
        if (resultItem != null)
        {
            Inventory.PutItem(resultItem);
            UIEvents.EvProfileChangeRequested(DisplayUIManager.ProfileState.Success, 1.0f);
        }

        FlushCraftZone();
        PlayEvents.EvCraftCompleted(Animator.StringToHash(craftedItemName));
    }

    private void RefreshContentHeight()
    {
        if (_contentRect == null)
            return;

        GridLayoutGroup gridLayout = _contentRect.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
            return;

        int itemCount = _itemRecipes.Count;
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


