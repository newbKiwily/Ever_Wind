
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CraftItemRecipe : ScriptableObject
{
    public string ResultItemName;

    [System.Serializable]
    public struct Ingredient
    {
        public InventoryItem IngredientItem;
        public int Amount;
    }

    public List<Ingredient> Ingredients;

}
