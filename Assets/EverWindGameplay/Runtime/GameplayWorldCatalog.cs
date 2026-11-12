using System;
using UnityEngine;
namespace EverWind.Gameplay
{
[CreateAssetMenu(menuName = "EverWind/World Catalog")]
public sealed class GameplayWorldCatalog : ScriptableObject
{
    public LevelDefinition[] Levels = Array.Empty<LevelDefinition>();
    public LevelDefinition Get(int id)
    {
        foreach (var level in Levels)
            if (level.Id == id)
                return level;
        return null;
    }
}
}
