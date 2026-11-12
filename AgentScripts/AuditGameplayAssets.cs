using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using EverWind.Gameplay;
public static class AuditGameplayAssets
{
    public static string Run()
    {
        const string root = "Assets/EverWindGameplay/Generated";
        int prefabs = 0, clips = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { root }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Any(c=>!prefab.name.StartsWith("Map") || c.gameObject.layer!=8) ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new Exception("Visual prefab contains gameplay/physics components: " + prefab.name);
            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true))
                if (animator.applyRootMotion || animator.runtimeAnimatorController != null)
                    throw new Exception("Visual prefab retained animation authority: " + prefab.name);
            ++prefabs;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { root }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (AnimationUtility.GetAnimationEvents(clip).Length != 0)
                throw new Exception("Animation events remain: " + clip.name);
            ++clips;
        }
        var profiles = AssetDatabase.FindAssets("t:ActionVisualProfile", new[] { root });
        foreach (var guid in profiles)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(AssetDatabase.GUIDToAssetPath(guid));
            if (profile.Controller == null || profile.Clips.Length < 9 || profile.Clips.Any(c => c.Clip == null))
                throw new Exception("Incomplete animation profile: " + profile.name);
        }
        string result =
            $"PASS: {prefabs} visual shells, {clips} event-free clips, {profiles.Length} replaceable profiles";
        System.IO.File.WriteAllText(".utmp/gameplay/asset-audit.txt", result);
        return result;
    }
}
