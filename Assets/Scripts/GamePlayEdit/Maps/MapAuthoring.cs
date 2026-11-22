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
        public Vector3 PlayerSpawn = new Vector3(DefaultMapSettings.Center.x, 0, DefaultMapSettings.Center.y);
        public Vector2 BoundaryCenter = DefaultMapSettings.Center;
        [Min(1)] public float Radius = DefaultMapSettings.Radius;
        [Min(0)] public float RespawnSeconds = 10;
        public Vector2 HeightOrigin = DefaultMapSettings.HeightOrigin;
        public Vector2 HeightSize = DefaultMapSettings.HeightSize;
        [Min(.1f)] public float HeightStep = DefaultMapSettings.HeightStep;
        public bool SnapOnMove = true;
    }
}
#endif
