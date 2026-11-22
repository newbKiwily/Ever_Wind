using System;
using UnityEditor;
using UnityEngine;

namespace EverWind.EntityEditing
{
    public enum EntityEditorKind { Resource, Player, Monster }

    [Serializable]
    public sealed class EntityEditorData
    {
        public EntityEditorKind Kind;
        public string Name = "NewEntity";
        public GameObject Model;
        public Avatar Avatar;
        public SceneAsset RegistrationScene;
        public int DefinitionId = 1, VisualId, ItemId = 1, Capacity = 1;
        public int Attack = 10, Defence, MaxHp = 100, Experience = 10, AttackSkillId = 1;
        public float MoveSpeed = 2.5f, Radius = .5f, GatherTime = 5, RefillDelay = 10, AttackCooldown = 1.8f;
        public Vector3 VisualOffset, VisualEuler;
        public float VisualScale = 1, DeadHeightOffset, SwingCue = .2f, AttackImpactNormalized = .45f;
        public GameObject SwingEffect, HitEffect;
        public Vector3 EffectOffset = Vector3.up;
        public AnimationClip Idle, Run, CombatIdle, CombatRun, Gather, Craft, Hit, Knockback, Dead, Success;
        public AnimationClip[] Skills = new AnimationClip[5];
        public bool ReplaceResourceVisual;
        public bool AlignRendererBottom = true, OverrideRespawn;
        public float HeightOffset, RespawnSeconds = 10;

        public string Folder => "Assets/Scripts/GamePlayEdit/Entities/" + Kind + "/" + Name;
        public bool Alive => Kind != EntityEditorKind.Resource;

        public AnimationClip Clip(string state)
        {
            if (state.StartsWith("Skill") && int.TryParse(state.Substring(5), out int slot))
                return Skills[slot - 1] != null ? Skills[slot - 1] : Skills[0];
            switch (state)
            {
                case "Idle": return Idle;
                case "Run": return Run;
                case "CombatIdle": return CombatIdle != null ? CombatIdle : Idle;
                case "CombatRun": return CombatRun != null ? CombatRun : Run;
                case "Gather": return Gather != null ? Gather : Idle;
                case "Craft": return Craft != null ? Craft : Gather != null ? Gather : Idle;
                case "Hit": return Hit;
                case "Knockback": return Knockback != null ? Knockback : Hit;
                case "Dead": return Dead;
                case "Success": return Success != null ? Success : Idle;
                default: throw new InvalidOperationException("지원하지 않는 Animator 상태: " + state);
            }
        }
    }
}
