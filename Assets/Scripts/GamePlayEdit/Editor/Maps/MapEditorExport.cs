using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EverWind.MapEditing
{
    public static class MapEditorExport
    {
        public const string MapsFolder = "Assets/Scripts/GamePlayEdit/Maps";
        public static string ContentPath => Path.GetFullPath("EverWindServer/LoginServer/Content/GameData.ewdata");
        static string F(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);

        public static HashSet<int> Definitions(string file, MapPlacementKind kind)
        {
            string prefix = kind == MapPlacementKind.Monster ? "monster" : "resource";
            var ids = new HashSet<int>();
            foreach (string line in File.ReadLines(file))
            {
                string[] p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length > 1 && p[0] == prefix && int.TryParse(p[1], out int id)) ids.Add(id);
            }
            return ids;
        }

        public static Bounds LandBounds(MapAuthoring map)
        {
            var colliders = Ground(map);
            Bounds result = colliders[0].bounds;
            foreach (var c in colliders) result.Encapsulate(c.bounds);
            return result;
        }

        static Collider[] Ground(MapAuthoring map)
        {
            if (map == null || map.Land == null) throw new InvalidOperationException("Land 루트를 지정하세요.");
            var list = map.Land.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            if (list.Length == 0) throw new InvalidOperationException("Land에 활성 Collider가 필요합니다.");
            return list;
        }

        public static bool GroundPoint(MapAuthoring map, Ray ray, out Vector3 point)
        {
            point = default;
            float closest = float.MaxValue;
            foreach (var c in Ground(map))
                if (c.Raycast(ray, out RaycastHit hit, 100000) && hit.distance < closest)
                { closest = hit.distance; point = hit.point; }
            return closest < float.MaxValue;
        }

        public static float Height(MapAuthoring map, Vector3 at)
        {
            Bounds b = LandBounds(map);
            var ray = new Ray(new Vector3(at.x, b.max.y + 100, at.z), Vector3.down);
            if (!GroundPoint(map, ray, out Vector3 point))
                throw new InvalidOperationException($"Land가 없는 위치입니다: X={at.x}, Z={at.z}. 높이 영역이나 배치를 확인하세요.");
            return point.y;
        }

        public static void Snap(MapAuthoring map, MapPlacement placement, bool undo)
        {
            Vector3 p = placement.transform.position;
            float bottom = 0;
            if (placement.AlignRendererBottom)
            {
                var renderers = placement.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
                if (renderers.Length > 0) bottom = renderers.Min(r => r.bounds.min.y) - p.y;
            }
            p.y = Height(map, p) - bottom + placement.HeightOffset;
            if ((p - placement.transform.position).sqrMagnitude < .000001f) return;
            if (undo) Undo.RecordObject(placement.transform, "Land 높이 맞춤");
            placement.transform.position = p;
            EditorUtility.SetDirty(placement.transform);
        }

        // 검증과 높이 샘플링을 끝낸 후에만 결과 파일을 만든다. 새 프리팹 폴더는 기존 결과와 충돌하지 않는다.
        public static string Export(MapAuthoring map, string txtPath, string folderName, string contentPath)
        {
            if (map == null || EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드의 작업 맵을 선택하세요.");
            Physics.SyncTransforms();
            if (map.transform.position != Vector3.zero || Quaternion.Angle(map.transform.rotation, Quaternion.identity) > .001f || map.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("작업 맵 루트의 위치/회전/크기는 0/0/1을 유지하세요. 자식 오브젝트를 편집하세요.");
            if (map.MapId < 0 || map.Version < 1 || !Finite(map.Radius) || map.Radius <= 0 || !Finite(map.RespawnSeconds) || map.RespawnSeconds < 0)
                throw new InvalidOperationException("맵 ID, 버전, 반경, 리스폰 시간을 확인하세요.");
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
                throw new InvalidOperationException("폴더명은 글자·숫자·밑줄·하이픈으로 입력하세요.");
            if (!Finite(map.HeightStep) || map.HeightStep < .1f || !Finite(map.HeightSize.x) || !Finite(map.HeightSize.y) || map.HeightSize.x <= 0 || map.HeightSize.y <= 0)
                throw new InvalidOperationException("높이 영역과 간격을 확인하세요.");
            int cols = Mathf.CeilToInt(map.HeightSize.x / map.HeightStep) + 1;
            int rows = Mathf.CeilToInt(map.HeightSize.y / map.HeightStep) + 1;
            if (cols < 2 || rows < 2 || cols > 2048 || rows > 2048) throw new InvalidOperationException("높이 격자는 각 축 2~2048개여야 합니다.");
            if (!Finite(map.HeightOrigin.x) || !Finite(map.HeightOrigin.y) || !Finite(map.BoundaryCenter.x) || !Finite(map.BoundaryCenter.y)) throw new InvalidOperationException("좌표는 유한한 값이어야 합니다.");
            var placements = map.GetComponentsInChildren<MapPlacement>();
            var ids = new HashSet<int>();
            var monsters = Definitions(contentPath, MapPlacementKind.Monster);
            var resources = Definitions(contentPath, MapPlacementKind.Resource);
            foreach (var p in placements)
            {
                if (p.Kind != MapPlacementKind.Decoration && (p.InstanceId <= 0 || !ids.Add(p.InstanceId))) throw new InvalidOperationException("스폰 개체 ID가 0 이하이거나 중복됩니다.");
                if (p.Kind == MapPlacementKind.Monster && !monsters.Contains(p.DefinitionId) || p.Kind == MapPlacementKind.Resource && !resources.Contains(p.DefinitionId))
                    throw new InvalidOperationException($"없는 서버 정의 ID입니다: {p.name} / {p.DefinitionId}");
                if (p.OverrideRespawn && (!Finite(p.RespawnSeconds) || p.RespawnSeconds < 0)) throw new InvalidOperationException("리스폰 시간을 확인하세요.");
                Vector3 v = p.transform.position;
                if (!Finite(v.x) || !Finite(v.z) || Vector2.Distance(new Vector2(v.x,v.z),map.BoundaryCenter) > map.Radius)
                    throw new InvalidOperationException("배치물이 맵 경계 밖에 있습니다: " + p.name);
                Height(map, v);
            }
            float[] heights = new float[cols * rows];
            for (int z = 0; z < rows; z++) for (int x = 0; x < cols; x++)
                heights[z * cols + x] = Height(map, new Vector3(map.HeightOrigin.x + x * map.HeightStep, 0, map.HeightOrigin.y + z * map.HeightStep));
            Vector3 spawn = map.PlayerSpawn;
            if (!Finite(spawn.x) || !Finite(spawn.z) || Vector2.Distance(new Vector2(spawn.x,spawn.z),map.BoundaryCenter) > map.Radius) throw new InvalidOperationException("플레이어 스폰이 경계 밖입니다.");
            spawn.y = Height(map, spawn);
            foreach (var p in placements) Snap(map, p, true);
            string full = Path.GetFullPath(txtPath);
            if (!full.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("TXT 파일을 지정하세요.");
            string heightName = "Map" + map.MapId + "_" + Guid.NewGuid().ToString("N") + ".heights";
            string heightPath = Path.Combine(Path.GetDirectoryName(full), heightName);
            var text = new StringBuilder();
            int m = map.MapId;
            text.AppendLine($"map {m} {F(map.Radius)}");
            text.AppendLine($"version {m} {map.Version}");
            text.AppendLine($"respawn {m} {F(map.RespawnSeconds)}");
            text.AppendLine($"spawn {m} {F(spawn.x)} {F(spawn.y)} {F(spawn.z)} {F(map.BoundaryCenter.x)} {F(map.BoundaryCenter.y)}");
            text.AppendLine($"surface {m} {F(map.HeightOrigin.x)} {F(map.HeightOrigin.y)} {F(map.HeightStep)} {cols} {rows} {heightName}");
            foreach (var p in placements.OrderBy(p => p.InstanceId))
            {
                Vector3 v = p.transform.position;
                string line = $"{m} {p.InstanceId} {p.DefinitionId} {F(v.x)} {F(v.z)}";
                if (p.Kind == MapPlacementKind.Monster) text.AppendLine("monsterspawn " + line + (p.OverrideRespawn ? " " + F(p.RespawnSeconds) : ""));
                if (p.Kind == MapPlacementKind.Resource) text.AppendLine("resourcespawn " + line);
            }
            int boxId = 1000000;
            foreach (var c in map.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger))
            {
                if (c.transform == map.Land || c.transform.IsChildOf(map.Land) || c.GetComponentInParent<MapPlacement>() is MapPlacement marker && marker.Kind != MapPlacementKind.Decoration) continue;
                Bounds b = c.bounds;
                while (ids.Contains(boxId)) boxId++;
                text.AppendLine($"box {m} {boxId++} {F(b.center.x)} {F(b.center.z)} {F(b.extents.x)} {F(b.extents.z)}");
            }
            string folder = AssetDatabase.GenerateUniqueAssetPath(MapsFolder + "/" + folderName);
            string prefabPath = folder + "/Map" + m + "Visual.prefab";
            GameObject copy = null;
            string temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            bool committed = false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                copy = UnityEngine.Object.Instantiate(map.gameObject);
                copy.name = "Map" + m + "Visual";
                var authoring = copy.GetComponent<MapAuthoring>();
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0) foreach (var c in authoring.Land.GetComponentsInChildren<Collider>(true)) c.gameObject.layer = groundLayer;
                foreach (var p in copy.GetComponentsInChildren<MapPlacement>(true))
                {
                    if (p.Kind == MapPlacementKind.Decoration) UnityEngine.Object.DestroyImmediate(p);
                    else UnityEngine.Object.DestroyImmediate(p.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(copy.GetComponent<MapAuthoring>());
                AssetDatabase.CreateFolder(MapsFolder, Path.GetFileName(folder));
                if (PrefabUtility.SaveAsPrefabAsset(copy, prefabPath) == null) throw new IOException("프리팹 생성 실패");
                using (var writer = new BinaryWriter(File.Create(heightPath))) foreach (float h in heights) writer.Write(h);
                File.WriteAllText(temp, text.ToString(), new UTF8Encoding(false));
                if (File.Exists(full)) File.Replace(temp, full, null); else File.Move(temp, full);
                committed = true;
                return prefabPath;
            }
            finally
            {
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
                if (File.Exists(temp)) File.Delete(temp);
                if (!committed)
                {
                    if (File.Exists(heightPath)) File.Delete(heightPath);
                    if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                }
                AssetDatabase.Refresh();
            }
        }
    }
}
