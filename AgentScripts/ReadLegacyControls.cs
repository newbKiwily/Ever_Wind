using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class ReadLegacyControls
{
    public static string Run()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/InGame.unity");
        try
        {
            var roots = scene.GetRootGameObjects();
            var legacy = roots.SelectMany(r => r.GetComponentsInChildren<CameraMoving>(true)).First();
            var camera = legacy.GetComponent<Camera>();
            var target = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "TargetingUI");
            object Read(string name) => typeof(CameraMoving).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(legacy);
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                legacy.RotSensitive, legacy.ZoomSensitive,
                distance = Read("_dis"), minPitch = Read("_rotationMin"), maxPitch = Read("_rotationMax"),
                smoothTime = Read("_smoothTime"), minDistance = Read("_minDis"), maxDistance = Read("_maxDis"),
                camera.fieldOfView, camera.nearClipPlane, camera.farClipPlane,
                camera.orthographic, camera.orthographicSize,
                euler = new[] { camera.transform.eulerAngles.x, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z },
                position = new[] { camera.transform.position.x, camera.transform.position.y, camera.transform.position.z },
                targetName = legacy.Target != null ? legacy.Target.name : null,
                targetIndicator = target != null ? target.GetComponentsInChildren<Component>(true).Select(c => c.GetType().Name).ToArray() : null
            });
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
