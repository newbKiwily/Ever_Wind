using System;
using EverWind.Gameplay;
using UnityEngine;

namespace EverWind.ItemEditing
{
    public enum ItemRecipeEditorMode { Item, Recipe }

    [Serializable]
    public sealed class ItemRecipeEditorData
    {
        public ItemRecipeEditorMode Mode;
        public GameplayCatalog Catalog;
        public InventoryItem Item, SourceItem;
        public CraftItemRecipe Recipe, SourceRecipe;
        public int Id = 1, GatherEventKey, CraftEventKey;
        public bool Gatherable;
        public int ResourceId = 1, SourceResourceId, Capacity = 1;
        public float Radius = .25f, GatherTime = 5, RefillDelay = 10;
        public GameObject ResourceModel;
        public Vector3 VisualOffset, VisualEuler;
        public float VisualScale = 1, HeightOffset;
        public bool AlignRendererBottom = true;
        public string RecipeName = "NewRecipe";
        public float CraftTime = 2.5f;
        public int OutputCount = 1;

        public string ItemKey => Item != null ? Item.ItemName : "";
        public string ItemPath => SourceItem != null ? UnityEditor.AssetDatabase.GetAssetPath(SourceItem) : "Assets/Scripts/GamePlayEdit/Items/Definitions/" + ItemKey + ".asset";
        public string RecipePath => SourceRecipe != null ? UnityEditor.AssetDatabase.GetAssetPath(SourceRecipe) : "Assets/Scripts/GamePlayEdit/Crafting/Recipes/" + RecipeName + ".asset";
        public string VisualFolder => "Assets/Scripts/GamePlayEdit/Items/Visuals/Generated/" + ItemKey;
        public string MapPlacementPath => VisualFolder + "/" + ItemKey + "Resource" + ResourceId + "MapPlacement.prefab";
    }
}
