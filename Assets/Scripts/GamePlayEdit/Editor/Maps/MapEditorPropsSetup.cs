using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EverWind.MapEditing
{
    public static class MapEditorPropsSetup
    {
        public const string Folder = MapEditorExport.MapsFolder + "/Props";
        const string Source = MapEditorExport.MapsFolder + "/Map1/Map1Visual.prefab";

        [MenuItem("Tools/EverWind/Import Map1 Props")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드에서 실행하세요.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(MapEditorExport.MapsFolder, "Props");
            var scene = EditorSceneManager.NewPreviewScene();
            var map = PrefabUtility.LoadPrefabContents(Source);
            try
            {
                Add("Monster_Skeleton", "Assets/Scripts/GamePlayEdit/Characters/Monsters/Skeleton/SkeletonVisual.prefab", MapPlacementKind.Monster, 1);
                Add("Monster_Spider", "Assets/Scripts/GamePlayEdit/Characters/Monsters/Spider/SpiderVisual.prefab", MapPlacementKind.Monster, 2);
                Add("Resource_Branch", "Assets/Scripts/GamePlayEdit/Items/Visuals/Resource1.prefab", MapPlacementKind.Resource, 1);
                Add("Resource_Grass", "Assets/Scripts/GamePlayEdit/Items/Visuals/Resource6.prefab", MapPlacementKind.Resource, 2);
                Add("Resource_Rock", "Assets/Scripts/GamePlayEdit/Items/Visuals/Resource2.prefab", MapPlacementKind.Resource, 3);
                var seen = new HashSet<string>();
                foreach (string group in new[] { "Buildings", "Rocks", "Path", "Vegetation", "Props", "Exterior" })
                {
                    Transform parent = map.transform.Find(group);
                    if (parent == null) continue;
                    foreach (Transform child in parent)
                    {
                        string name = Regex.Replace(child.name, @"\s*\(\d+\)$", "");
                        string key = "Static_" + Regex.Replace(name, @"[^\w-]", "_");
                        if (!seen.Add(key) || AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + key + ".prefab") != null) continue;
                        var copy = UnityEngine.Object.Instantiate(child.gameObject);
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, scene);
                        copy.name = key;
                        copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        var marker = copy.GetComponent<MapPlacement>() ?? copy.AddComponent<MapPlacement>();
                        marker.Kind = MapPlacementKind.Decoration;
                        PrefabUtility.SaveAsPrefabAsset(copy, Folder + "/" + key + ".prefab");
                        UnityEngine.Object.DestroyImmediate(copy);
                    }
                }
                void Add(string name, string source, MapPlacementKind kind, int definition)
                {
                    string target = Folder + "/" + name + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(target) != null) return;
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(source);
                    if (asset == null) throw new FileNotFoundException(source);
                    var root = new GameObject(name);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                    visual.transform.SetParent(root.transform, false);
                    var marker = root.AddComponent<MapPlacement>();
                    marker.Kind = kind; marker.DefinitionId = definition;
                    PrefabUtility.SaveAsPrefabAsset(root, target);
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(map);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
