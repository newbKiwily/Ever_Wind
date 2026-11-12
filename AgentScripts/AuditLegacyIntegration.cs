using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class AuditLegacyIntegration
{
    static string PathOf(Transform t) => t.parent != null ? PathOf(t.parent) + "/" + t.name : t.name;
    public static string Run()
    {
        Directory.CreateDirectory(".utmp/migration");
        var result = new List<object>();
        var maps = new List<object>();
        foreach (string path in new[] { "Assets/Scenes/LogIn.unity", "Assets/Scenes/InGame.unity" })
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var roots = scene.GetRootGameObjects();
                foreach (var dc in roots.SelectMany(r => r.GetComponentsInChildren<DataCenter>(true)))
                    foreach (var pair in dc.MapTable)
                        maps.Add(new { id = pair.Key, prefab = AssetDatabase.GetAssetPath(pair.Value.MapPrefab),
                            minimap = AssetDatabase.GetAssetPath(pair.Value.MinimapImage),
                            position = pair.Value.Position.ToString("F3"), rotation = pair.Value.Rotation.ToString("F3"), scale = pair.Value.Scale });
                var behaviours = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(b => b != null);
                result.Add(new { scene = path, roots = roots.Select(r => r.name).ToArray(), behaviours = behaviours.Select(b => {
                    var refs = new Dictionary<string, string>();
                    var so = new SerializedObject(b);
                    var it = so.GetIterator();
                    while (it.Next(true)) if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                        refs[it.propertyPath] = AssetDatabase.GetAssetPath(it.objectReferenceValue) + " : " + it.objectReferenceValue.name;
                    return new { path = PathOf(b.transform), type = b.GetType().FullName, references = refs };
                }).ToArray() });
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        File.WriteAllText(".utmp/migration/legacy-scenes.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { scenes = result, maps }, Newtonsoft.Json.Formatting.Indented));
        return "Saved legacy scene/UI/map reference inventory";
    }
}
