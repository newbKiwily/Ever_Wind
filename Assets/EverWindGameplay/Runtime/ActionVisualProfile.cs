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
    public AnimationClip[] Skills = new AnimationClip[5];

    public GameObject SwingEffect, HitEffect;

    public Vector3 EffectOffset = new Vector3(0, 1, 0);

    public float SwingCue = .2f;

    [Range(.05f, .95f)]
    public float AttackImpactNormalized = .45f;

    public AnimationClip ResolveClip(string state)
    {
        string source = state;

        if (state.StartsWith("Skill") && int.TryParse(state.Substring(5), out int slot))
        {
            if (Skills != null && slot >= 1 && slot <= Skills.Length && Skills[slot - 1] != null)
            {
                return Skills[slot - 1];
            }

            source = Attacks.Length > 0 ? Attacks[Mathf.Clamp(slot - 1, 0, Attacks.Length - 1)] : Idle;
        }
        else if (state == "Craft")
        {
            source = Gather;
        }
        else if (state == "Hit" || state == "Knockback")
        {
            source = Damaged;
        }
        else if (state == "Idle")
        {
            source = Idle;
        }
        else if (state == "Run")
        {
            source = Run;
        }
        else if (state == "CombatIdle")
        {
            source = CombatIdle;
        }
        else if (state == "CombatRun")
        {
            source = CombatRun;
        }
        else if (state == "Dead")
        {
            source = Dead;
        }
        else if (state == "Gather")
        {
            source = Gather;
        }

        foreach (var entry in Clips)
        {
            if (entry.State == source && entry.Clip != null)
            {
                return entry.Clip;
            }
        }

        foreach (var entry in Clips)
        {
            if (entry.State == Idle && entry.Clip != null)
            {
                return entry.Clip;
            }
        }

        return null;
    }
}
}
