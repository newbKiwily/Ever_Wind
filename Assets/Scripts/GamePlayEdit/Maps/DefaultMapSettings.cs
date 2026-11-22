#if UNITY_EDITOR
using UnityEngine;

namespace EverWind.MapEditing
{
    // DefaultMap과 Map0/Map1의 공통 편집 기준. 산맥이나 프랍의 Bounds로 다시 계산하지 않는다.
    public static class DefaultMapSettings
    {
        public static readonly Vector2 Center = new Vector2(300, -80);
        public const float Radius = 110;
        public static readonly Vector2 HeightOrigin = new Vector2(190, -190);
        public static readonly Vector2 HeightSize = new Vector2(220, 220);
        public const float HeightStep = .5f;

        // 맵별 스폰, 배치물, ID와 그 밖의 설정은 유지한다.
        public static void ApplyTo(MapAuthoring map)
        {
            map.BoundaryCenter = Center;
            map.Radius = Radius;
            map.HeightOrigin = HeightOrigin;
            map.HeightSize = HeightSize;
            map.HeightStep = HeightStep;
        }
    }
}
#endif
