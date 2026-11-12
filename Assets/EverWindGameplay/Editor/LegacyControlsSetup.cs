using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EverWind.Gameplay;
using Object = UnityEngine.Object;

namespace EverWind.Gameplay.Editor
{
public static class LegacyControlsSetup
{
    public static void Configure(ServerTransceiverManager registry, Camera camera)
    {
        var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/InGame.unity");
        try
        {
            var roots = preview.GetRootGameObjects();
            var source = roots.SelectMany(r => r.GetComponentsInChildren<CameraMoving>(true)).FirstOrDefault();
            if(source==null){var existing=roots.SelectMany(r=>r.GetComponentsInChildren<GameplayCameraController>(true)).First();
                var copy=camera.GetComponent<GameplayCameraController>()??camera.gameObject.AddComponent<GameplayCameraController>();
                EditorUtility.CopySerialized(existing,copy);copy.Registry=registry;
                registry.TargetIndicatorVisual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EverWindGameplay/Generated/TargetIndicatorVisual.prefab");return;}
            float Value(string name) => (float)typeof(CameraMoving)
                                            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                                            .GetValue(source);
            camera.CopyFrom(source.GetComponent<Camera>());
            var control = camera.GetComponent<GameplayCameraController>();
            if (control == null)
                control = camera.gameObject.AddComponent<GameplayCameraController>();
            control.Registry = registry;
            control.RotationSensitivity = source.RotSensitive;
            control.ZoomSensitivity = source.ZoomSensitive;
            control.InitialDistance = Value("_dis");
            control.MinDistance = Value("_minDis");
            control.MaxDistance = Value("_maxDis");
            control.MinPitch = Value("_rotationMin");
            control.MaxPitch = Value("_rotationMax");
            control.SmoothTime = Value("_smoothTime");
            control.InitialAngles = new Vector2(Value("_xaxis"), Value("_yaxis"));
            camera.transform.rotation = Quaternion.Euler(control.InitialAngles.x, control.InitialAngles.y, 0);
            camera.transform.position = new Vector3(0, 0, -10) - camera.transform.forward *control.InitialDistance;
            var indicator =
                roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).First(t => t.name == "TargetingUI");
            if (indicator.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Legacy target indicator unexpectedly contains behaviour scripts");
            registry.TargetIndicatorVisual = PrefabUtility.SaveAsPrefabAsset(
                indicator.gameObject, "Assets/EverWindGameplay/Generated/TargetIndicatorVisual.prefab");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
    public static string Run()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop play mode before applying scene settings");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/InGame.unity")
            throw new InvalidOperationException("Open InGame first");
        var registry = Object.FindFirstObjectByType<ServerTransceiverManager>();
        Configure(registry, Camera.main);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Applied original camera and target indicator settings to InGame";
    }
}
}
