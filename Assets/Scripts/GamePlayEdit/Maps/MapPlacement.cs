#if UNITY_EDITOR
using UnityEngine;

namespace EverWind.MapEditing
{
    public enum MapPlacementKind { Decoration, Monster, Resource }

    [DisallowMultipleComponent]
    public sealed class MapPlacement : MonoBehaviour
    {
        public MapPlacementKind Kind;
        public int DefinitionId = 1;
        public int InstanceId;
        public bool AlignRendererBottom = true;
        public float HeightOffset;
        public bool OverrideRespawn;
        [Min(0)] public float RespawnSeconds = 10;
    }
}
#endif
