#if UNITY_EDITOR
using UnityEngine;

namespace EverWind.MapEditing
{
    [DisallowMultipleComponent]
    public sealed class MapAuthoring : MonoBehaviour
    {
        public int MapId = 2;
        public int Version = 1;
        public Transform Land;
        public Transform Geometry;
        public Transform Placements;
        public Vector3 PlayerSpawn;
        public Vector2 BoundaryCenter;
        [Min(1)] public float Radius = 50;
        [Min(0)] public float RespawnSeconds = 10;
        public Vector2 HeightOrigin = new Vector2(-50, -50);
        public Vector2 HeightSize = new Vector2(100, 100);
        [Min(.1f)] public float HeightStep = 1;
        public bool SnapOnMove = true;
    }
}
#endif
