using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EverWind.MapEditing
{
    public sealed class MapEditorWindow : EditorWindow
    {
        [SerializeField] MapAuthoring map;
        [SerializeField] string folderName = "NewMap";
        [SerializeField] string contentPath;
        Vector2 scroll;
        string feedback;
        bool preview;
        double previousTime;
        float previewTime;
        ParticleSystem[] particles;
        Animator[] animators;
        public const string DefaultMapPath = MapEditorExport.MapsFolder + "/Common/DefaultMap.prefab";

        [MenuItem("Tools/EverWind/Map Editor")]
        public static void Open() => GetWindow<MapEditorWindow>("EverWind 맵 편집기");

        void OnEnable()
        {
            minSize = new Vector2(350, 540);
            if (string.IsNullOrEmpty(contentPath)) contentPath = MapEditorExport.ContentPath;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += Repaint;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        void OnDisable()
        {
            StopPreview();
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= Repaint;
            EditorApplication.playModeStateChanged -= OnPlayMode;
        }

        void OnPlayMode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingEditMode) StopPreview(); }

        void Run(Action action)
        {
            try { action(); feedback = "완료"; }
            catch (Exception e) { feedback = e.Message; }
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("맵 제작 · 배치 · 추출", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Project의 프리팹을 Scene 뷰의 Land 위로 드래그해서 배치하세요. 용도는 프리팹 MapPlacement에서 가져옵니다. 작업 씬을 저장하면 배치 설정도 보존됩니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                map = (MapAuthoring)EditorGUILayout.ObjectField("작업 맵", map, typeof(MapAuthoring), true);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새 작업 씬")) Run(CreateMap);
                    if (GUILayout.Button("선택한 맵 사용")) Run(() =>
                    {
                        map = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<MapAuthoring>() : null;
                        if (map == null) throw new InvalidOperationException("MapAuthoring이 있는 작업 맵을 선택하세요.");
                    });
                    if (GUILayout.Button("Scene 보기")) FocusMap();
                }
                if (map == null) return;
                if (EditorUtility.IsPersistent(map)) { EditorGUILayout.HelpBox("프리팹 에셋 대신 작업 씬의 맵을 선택하세요.", MessageType.Warning); return; }
                scroll = EditorGUILayout.BeginScrollView(scroll);
                var serialized = new SerializedObject(map);
                serialized.Update();
                foreach (string name in new[] { "MapId", "Version", "Land", "Geometry", "Placements", "PlayerSpawn", "BoundaryCenter", "Radius", "RespawnSeconds", "HeightOrigin", "HeightSize", "HeightStep", "SnapOnMove" })
                    EditorGUILayout.PropertyField(serialized.FindProperty(name), new GUIContent(Label(name)));
                serialized.ApplyModifiedProperties();
                if (GUILayout.Button("DefaultMap 기준 적용")) Run(ApplyDefaultMapSettings);
                EditorGUILayout.Space();
                if (GUILayout.Button("모든 배치물 Land 높이 맞춤")) Run(() =>
                {
                    StopPreview(); Physics.SyncTransforms();
                    foreach (var p in map.GetComponentsInChildren<MapPlacement>()) MapEditorExport.Snap(map, p, true);
                    EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
                });
                if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<MapPlacement>() is MapPlacement selected && selected.transform.IsChildOf(map.transform))
                {
                    EditorGUILayout.LabelField("선택 배치물: " + selected.name, EditorStyles.boldLabel);
                    var marker = new SerializedObject(selected);
                    marker.Update();
                    foreach (string name in new[] { "Kind", "DefinitionId", "InstanceId", "AlignRendererBottom", "HeightOffset", "OverrideRespawn", "RespawnSeconds" })
                    {
                        using (new EditorGUI.DisabledScope(name == "Kind"))
                            EditorGUILayout.PropertyField(marker.FindProperty(name), new GUIContent(Label(name)));
                    }
                    marker.ApplyModifiedProperties();
                }
                EditorGUILayout.Space();
                if (GUILayout.Button(preview ? "미리보기 중지" : "애니메이션·파티클 미리보기")) Run(() => { if (preview) StopPreview(); else StartPreview(); });
                EditorGUILayout.HelpBox("미리보기는 외형 재생입니다. 서버 AI·전투 시뮬레이션은 실행하지 않습니다.", MessageType.None);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("추출", EditorStyles.boldLabel);
                contentPath = EditorGUILayout.TextField("서버 기준 파일", contentPath);
                folderName = EditorGUILayout.TextField("Maps 하위 새 폴더", folderName);
                if (GUILayout.Button("서버 TXT 위치 선택 후 추출")) Run(Export);
                if (GUILayout.Button("작업 씬 저장")) Run(() => { StopPreview(); EditorSceneManager.SaveScene(map.gameObject.scene); });
                if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }

        static string Label(string name)
        {
            switch (name)
            {
                case "MapId": return "맵 ID"; case "Version": return "맵 버전";
                case "Land": return "Land (지면 Collider 루트)"; case "Geometry": return "고정 지형·건물 루트";
                case "Placements": return "배치물 루트"; case "PlayerSpawn": return "플레이어 스폰";
                case "BoundaryCenter": return "맵 중심 XZ"; case "Radius": return "맵 반경";
                case "RespawnSeconds": return "리스폰 시간"; case "HeightOrigin": return "높이 격자 시작 XZ";
                case "HeightSize": return "높이 격자 크기 XZ"; case "HeightStep": return "높이 격자 간격";
                case "SnapOnMove": return "이동 후 자동 높이 맞춤"; case "Kind": return "용도";
                case "DefinitionId": return "서버 정의 ID"; case "InstanceId": return "스폰 개체 ID";
                case "AlignRendererBottom": return "모델 바닥 기준 정렬"; case "HeightOffset": return "추가 높이 보정";
                case "OverrideRespawn": return "개별 리스폰 시간 사용"; default: return name;
            }
        }

        public void CreateMap()
        {
            var defaultMap = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMapPath);
            if (defaultMap == null) throw new FileNotFoundException("기본 월드가 없습니다: " + DefaultMapPath);
            StopPreview();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Map_Workspace");
            SceneManager.MoveGameObjectToScene(root, scene);
            map = root.AddComponent<MapAuthoring>();
            foreach (string name in new[] { "Land", "Geometry", "Placements" })
            {
                var child = new GameObject(name); child.transform.SetParent(root.transform, false);
                if (name == "Land") map.Land = child.transform;
                if (name == "Geometry") map.Geometry = child.transform;
                if (name == "Placements") map.Placements = child.transform;
            }
            var world = (GameObject)PrefabUtility.InstantiatePrefab(defaultMap, scene);
            world.transform.SetParent(map.Geometry, false);
            var terrain = world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Terrain");
            if (terrain == null) throw new InvalidOperationException("DefaultMap에 Terrain 루트가 필요합니다.");
            UnityEngine.Object.DestroyImmediate(map.Land.gameObject);
            map.Land = terrain;
            Physics.SyncTransforms();
            DefaultMapSettings.ApplyTo(map);
            var center = DefaultMapSettings.Center;
            map.PlayerSpawn = new Vector3(center.x, MapEditorExport.Height(map, new Vector3(center.x, 0, center.y)), center.y);
            Undo.RegisterCreatedObjectUndo(root, "새 맵");
            Selection.activeGameObject = root;
            FocusMap();
        }

        void FocusMap()
        {
            var view = EditorWindow.GetWindow<SceneView>(); view.Show();
            if (map != null)
            {
                Selection.activeGameObject = map.gameObject;
                view.Frame(new Bounds(map.PlayerSpawn, new Vector3(map.Radius * 2, 10, map.Radius * 2)), true);
            }
        }

        public void ApplyDefaultMapSettings()
        {
            if (map == null) throw new InvalidOperationException("작업 맵을 선택하세요.");
            Undo.RecordObject(map, "DefaultMap 기준 적용");
            DefaultMapSettings.ApplyTo(map);
            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        }

        void AssignId(MapPlacement marker)
        {
            if (marker.InstanceId > 0 || marker.Kind == MapPlacementKind.Decoration) return;
            int next = 10000;
            var ids = map.GetComponentsInChildren<MapPlacement>().Where(p => p != marker).Select(p => p.InstanceId).ToHashSet();
            while (ids.Contains(next)) next++;
            marker.InstanceId = next;
        }

        public GameObject Place(GameObject asset, Vector3 point)
        {
            if (asset == null || !PrefabUtility.IsPartOfPrefabAsset(asset)) throw new InvalidOperationException("프리팹 에셋을 지정하세요.");
            StopPreview();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, map.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "프리팹 배치");
            go.transform.SetParent(map.Placements, true); go.transform.position = point;
            var marker = go.GetComponent<MapPlacement>() ?? Undo.AddComponent<MapPlacement>(go);
            marker.InstanceId = 0;
            AssignId(marker);
            Physics.SyncTransforms(); MapEditorExport.Snap(map, marker, false);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            return go;
        }

        void OnSceneGUI(SceneView view)
        {
            if (map == null || map.Land == null || EditorApplication.isPlaying) return;
            var e = Event.current;
            if (!preview && map.SnapOnMove && e.type == EventType.MouseUp && e.button == 0)
                foreach (var p in Selection.transforms.Select(t => t.GetComponentInParent<MapPlacement>()).Where(p => p != null && p.transform.IsChildOf(map.transform)).Distinct())
                    Run(() => MapEditorExport.Snap(map, p, true));
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            var assets = DragAndDrop.objectReferences.OfType<GameObject>().Where(PrefabUtility.IsPartOfPrefabAsset).ToArray();
            if (assets.Length == 0) return;
            StopPreview();
            Vector3 point;
            try
            {
                if (!MapEditorExport.GroundPoint(map, HandleUtility.GUIPointToWorldRay(e.mousePosition), out point))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    e.Use();
                    return;
                }
            }
            catch (Exception error)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                if (e.type == EventType.DragPerform) feedback = error.Message;
                e.Use();
                return;
            }
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(point, Vector3.up, .5f);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var asset in assets) Run(() => Place(asset, point));
            }
            e.Use();
            view.Repaint();
        }

        public void StartPreview()
        {
            if (AnimationMode.InAnimationMode()) throw new InvalidOperationException("다른 애니메이션 미리보기를 먼저 종료하세요.");
            particles = map.GetComponentsInChildren<ParticleSystem>();
            animators = map.GetComponentsInChildren<Animator>().Where(a => a.runtimeAnimatorController != null).ToArray();
            AnimationMode.StartAnimationMode(); preview = true; previousTime = EditorApplication.timeSinceStartup; previewTime = 0;
        }

        public void StopPreview()
        {
            if (!preview) return;
            preview = false;
            AnimationMode.StopAnimationMode();
            if (particles != null) foreach (var p in particles) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles = null; animators = null; SceneView.RepaintAll();
        }

        void Tick()
        {
            if (!preview) return;
            if (map == null) { StopPreview(); return; }
            float dt = Mathf.Min(.05f, (float)(EditorApplication.timeSinceStartup - previousTime));
            previousTime = EditorApplication.timeSinceStartup; previewTime += dt;
            foreach (var p in particles) if (p != null && p.transform.parent?.GetComponentInParent<ParticleSystem>() == null) p.Simulate(dt, true, false);
            AnimationMode.BeginSampling();
            try
            {
                foreach (var a in animators) if (a != null)
                {
                    var clips = a.runtimeAnimatorController.animationClips;
                    var clip = clips.FirstOrDefault(c => c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0) ?? clips.FirstOrDefault();
                    if (clip != null && clip.length > 0) AnimationMode.SampleAnimationClip(a.gameObject, clip, previewTime % clip.length);
                }
            }
            finally { AnimationMode.EndSampling(); }
            SceneView.RepaintAll();
        }

        void Export()
        {
            StopPreview();
            string file = EditorUtility.SaveFilePanel("서버 맵 TXT 저장 위치", Path.GetFullPath("EverWindServer/LoginServer/Content/MapData/Maps"), "Map" + map.MapId, "txt");
            if (string.IsNullOrEmpty(file)) return;
            Physics.SyncTransforms();
            string result = MapEditorExport.Export(map, file, folderName, contentPath);
            EditorUtility.DisplayDialog("맵 추출 완료", "서버 맵: " + file + "\n클라이언트 프리팹: " + result, "확인");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(result));
        }
    }
}
