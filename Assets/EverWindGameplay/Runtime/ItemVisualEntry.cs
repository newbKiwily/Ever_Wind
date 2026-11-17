using System;
using UnityEngine;
namespace EverWind.Gameplay
{
[Serializable]
public sealed class ItemVisualEntry
{
    public int Item;
    public GameObject Resource, Placement;
    public UnityEngine.AddressableAssets.AssetReferenceGameObject ResourceReference, PlacementReference;
}
}
