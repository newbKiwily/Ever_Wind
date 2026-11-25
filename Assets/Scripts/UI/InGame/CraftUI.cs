using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class CraftUI : UIListPanel
{
    public Slot IngredientPrefab;
    public Transform IngredientContent;
    public TMP_Text SelectedName;
    public Image SelectedIcon;
    readonly Dictionary<int, Slot> ingredients = new Dictionary<int, Slot>();
    int selected;
    InGameUIState latest;
    protected override void OnEnable() { PlayEvents.UIRecipeSelected += Select; base.OnEnable(); }
    protected override void OnDisable() { PlayEvents.UIRecipeSelected -= Select; base.OnDisable(); }
    void Select(int id) { selected = id; if (latest != null) Apply(latest); }
    protected override void Apply(InGameUIState state)
    {
        latest = state;
        var data = new List<UISlotData>(); bool found = false;
        foreach (var recipe in state.Recipes)
        {
            data.Add(new UISlotData { Kind = UISlotKind.Recipe, Key = recipe.Id, Name = recipe.Name, Icon = recipe.Icon, Description = "클릭하여 제작법 선택", Interactable = true });
            found |= recipe.Id == selected;
        }
        if (!found) selected = state.Recipes.Length > 0 ? state.Recipes[0].Id : 0;
        Render(data);
        var parts = new List<UISlotData>(); bool available = false;
        SelectedName.text = "제작법을 선택하세요";
        if (SelectedIcon != null) SelectedIcon.enabled = false;
        foreach (var recipe in state.Recipes) if (recipe.Id == selected)
        {
            SelectedName.text = recipe.Name; available = recipe.Available;
            if (SelectedIcon != null) { SelectedIcon.sprite = recipe.Icon; SelectedIcon.enabled = recipe.Icon != null; }
            for (int i = 0; i < recipe.Ingredients.Length; ++i)
            {
                var item = recipe.Ingredients[i];
                parts.Add(new UISlotData { Kind = UISlotKind.Ingredient, Key = i, Name = item.Name, Description = item.Description, Icon = item.Icon, Count = item.Description, Interactable = true });
            }
        }
        RenderInto(parts, ingredients, IngredientContent, IngredientPrefab);
        PlayEvents.EvUIControlState(new UIControlState { Control = "craft:execute", Value = selected, Enabled = available });
    }
}
