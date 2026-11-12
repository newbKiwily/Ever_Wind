using System;
using UnityEngine;

namespace EverWind.Gameplay
{
[CreateAssetMenu(menuName = "EverWind/Action Visual Profile")]
public sealed class ActionVisualProfile : ScriptableObject
{
    public RuntimeAnimatorController Controller;
    public Vector3 VisualOffset;
    public Vector3 VisualEuler;
    public float VisualScale = 1;
    public string Idle = "Idle", Run = "Run", CombatIdle = "CombatIdle", CombatRun = "CombatRun",
                  Gather = "Gather", Damaged = "Damaged", Dead = "Dead";
    public string[] Attacks = { "Attack1", "Attack2" };
    [Serializable]
    public struct ClipEntry
    {
        public string State;
        public AnimationClip Clip;
    }
    public ClipEntry[] Clips = Array.Empty<ClipEntry>();
    public float Length(string state)
    {
        foreach (var entry in Clips)
            if (entry.State == state && entry.Clip != null)
                return entry.Clip.length;
        return 1;
    }
    public GameObject SwingEffect, HitEffect;
    public Vector3 EffectOffset = new Vector3(0, 1, 0);
    public float SwingCue = .2f;
    [Range(.05f, .95f)]
    public float AttackImpactNormalized = .45f;
}
}
