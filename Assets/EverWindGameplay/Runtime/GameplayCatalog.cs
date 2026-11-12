using System;
using UnityEngine;
namespace EverWind.Gameplay
{
[CreateAssetMenu(menuName = "EverWind/Gameplay Catalog")]
public sealed class GameplayCatalog : ScriptableObject
{
    [Serializable]
    public struct ItemEntry
    {
        public int Id;
        public InventoryItem Definition;
    }
    [Serializable]
    public struct RecipeEntry
    {
        public int Id;
        public CraftItemRecipe Definition;
    }
    [Serializable]
    public struct SkillEntry
    {
        public int Id;
        public float Reach;
        public float Cooldown;
    }
    public ItemEntry[] Items = Array.Empty<ItemEntry>();
    public RecipeEntry[] Recipes = Array.Empty<RecipeEntry>();
    public SkillEntry[] Skills = Array.Empty<SkillEntry>();
    public Quest[] Quests = Array.Empty<Quest>();
    public ItemVisualEntry[] Visuals = Array.Empty<ItemVisualEntry>();
    public InventoryItem Item(int id)
    {
        foreach (var entry in Items)
            if (entry.Id == id)
                return entry.Definition;
        return null;
    }
    public int ItemId(InventoryItem item)
    {
        foreach (var entry in Items)
            if (entry.Definition == item)
                return entry.Id;
        return 0;
    }
    public int RecipeId(CraftItemRecipe recipe)
    {
        foreach (var entry in Recipes)
            if (entry.Definition == recipe)
                return entry.Id;
        return 0;
    }
    public GameObject Visual(int id, bool placement)
    {
        foreach (var entry in Visuals)
            if (entry.Item == id)
                return placement ? entry.Placement : entry.Resource;
        return null;
    }
    public float Reach(int id)
    {
        foreach (var skill in Skills)
            if (skill.Id == id)
                return skill.Reach;
        return 0;
    }
    public float Cooldown(int id)
    {
        foreach (var skill in Skills)
            if (skill.Id == id)
                return skill.Cooldown;
        return 0;
    }
    public bool HasSkill(int id)
    {
        foreach (var skill in Skills)
            if (skill.Id == id)
                return true;
        return false;
    }
    public string ItemName(int id)
    {
        foreach (var item in Items)
            if (item.Id == id && item.Definition != null)
                return item.Definition.name;
        return id.ToString();
    }
}
}
