using System;
using System.Collections.Generic;
using EverWind.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace EverWind.SkillEditing
{
    // 실제 씬·원본 에셋·전역 AnimationMode를 건드리지 않는 전용 미리보기.
    public sealed class SkillEditorPreview : IDisposable
    {
        PreviewRenderUtility utility;
        GameObject character, ro, guides;
        readonly List<GameObject> effects = new List<GameObject>();
        readonly List<float> effectTimes = new List<float>();
        PlayableGraph graph;
        AnimationClipPlayable motion;
        Material lineMaterial;
        bool dirty = true;
        GameObject sourceModel;
        SkillDefinition sourceSkill;
        Vector2 orbit = new Vector2(145, 20);
        float distance = 7;
        Vector3 lookAt = new Vector3(0, 1, .8f);
        Vector3 motionPosition;
        Quaternion motionRotation;
        Animator animator;
        public void Invalidate() => dirty = true;
        public void ResetCamera() { orbit = new Vector2(145, 20); distance = 7; }

        public void Draw(Rect rect, GameObject model, SkillDefinition skill, float time, Vector3 target, bool shapes)
        {
            if (rect.width < 2 || rect.height < 2) return;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
            { GUIUtility.hotControl = control; evt.Use(); }
            if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == control)
            { orbit.x -= evt.delta.x; orbit.y = Mathf.Clamp(orbit.y + evt.delta.y, -5, 80); evt.Use(); }
            if (evt.type == EventType.MouseUp && GUIUtility.hotControl == control) { GUIUtility.hotControl = 0; evt.Use(); }
            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            { distance = Mathf.Clamp(distance * (1 + evt.delta.y * .05f), 1, 40); evt.Use(); }
            if (evt.type != EventType.Repaint) return;
            if (utility == null) CreateUtility();
            if (dirty || sourceModel != model || sourceSkill != skill) Rebuild(model, skill, target, shapes);
            Sample(skill, time);
            utility.camera.transform.rotation = Quaternion.Euler(orbit.y, orbit.x, 0);
            utility.camera.transform.position = lookAt - utility.camera.transform.forward * distance;
            utility.BeginPreview(rect, GUIStyle.none);
            Texture texture;
            try { utility.Render(true); }
            finally { texture = utility.EndPreview(); }
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        void CreateUtility()
        {
            utility = new PreviewRenderUtility();
            utility.camera.fieldOfView = 35;
            utility.camera.nearClipPlane = .01f; utility.camera.farClipPlane = 200;
            utility.camera.clearFlags = CameraClearFlags.SolidColor;
            utility.camera.backgroundColor = new Color(.14f, .16f, .2f);
            utility.lights[0].intensity = 1.2f; utility.lights[0].transform.rotation = Quaternion.Euler(40, 35, 0);
            utility.lights[1].intensity = .8f;
            utility.ambientColor = new Color(.5f, .5f, .5f);
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            lineMaterial.SetInt("_ZWrite", 0); lineMaterial.SetInt("_Cull", 0);
        }

        GameObject Clone(GameObject source, bool allowDetector = false)
        {
            if (source == null) return null;
            // 사용자 스크립트의 ExecuteAlways 실행을 피하기 위해 외형만 받는다.
            foreach (var script in source.GetComponentsInChildren<MonoBehaviour>(true))
                if (!allowDetector || !(script is SkillRangeDetector))
                    throw new InvalidOperationException("미리보기에는 스크립트가 없는 외형 전용 프리팹을 사용하세요.");
            var clone = Object.Instantiate(source);
            clone.hideFlags = HideFlags.HideAndDontSave;
            utility.AddSingleGO(clone);
            if (allowDetector)
                foreach (var detector in clone.GetComponentsInChildren<SkillRangeDetector>(true)) Object.DestroyImmediate(detector);
            foreach (var audio in clone.GetComponentsInChildren<AudioSource>(true)) { audio.Stop(); audio.enabled = false; }
            foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var a in clone.GetComponentsInChildren<Animator>(true)) { a.applyRootMotion = false; a.fireEvents = false; a.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            foreach (var particle in clone.GetComponentsInChildren<ParticleSystem>(true))
            { particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); particle.useAutoRandomSeed = false; particle.randomSeed = 1; }
            return clone;
        }

        void Rebuild(GameObject model, SkillDefinition skill, Vector3 target, bool shapes)
        {
            ClearObjects(); sourceModel = model; sourceSkill = skill;
            character = Clone(model);
            if (character != null)
            {
                character.transform.position = Vector3.zero; character.transform.rotation = Quaternion.identity;
                animator = character.GetComponentInChildren<Animator>();
                if (animator != null && skill.Animation != null)
                {
                    if (!animator.isHuman || !skill.Animation.isHumanMotion) throw new InvalidOperationException("Humanoid 캐릭터와 애니메이션이 필요합니다.");
                    animator.runtimeAnimatorController = null;
                    motionPosition = animator.transform.localPosition; motionRotation = animator.transform.localRotation;
                    graph = PlayableGraph.Create("EverWind Skill Preview"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    motion = AnimationClipPlayable.Create(graph, skill.Animation); motion.SetApplyFootIK(false); motion.SetApplyPlayableIK(false);
                    AnimationPlayableOutput.Create(graph, "Character", animator).SetSourcePlayable(motion); graph.Play();
                }
            }
            Vector3 center = skill is RangedSkill r ? Center(r, target) : Vector3.zero;
            if (skill is RangedSkill ranged && ranged.ROModel != null)
            { ro = Clone(ranged.ROModel); ro.transform.position = center; }
            else if (!(skill is RangedSkill) && skill.UsesRO && skill.RangeDetector != null)
            { ro = Clone(skill.RangeDetector.gameObject, true); ro.transform.position = center; }
            AddEffect(skill.CastEffect, skill.CastEffectTime, Vector3.up);
            AddEffect(skill.SwingEffect, skill.EffectDelay, skill.UsesRO ? center : Vector3.up);
            guides = new GameObject("Preview guides") { hideFlags = HideFlags.HideAndDontSave }; utility.AddSingleGO(guides);
            for (int i = -5; i <= 5; ++i)
            {
                Line(new[] { new Vector3(i, 0, -5), new Vector3(i, 0, 5) }, new Color(.26f, .29f, .34f));
                Line(new[] { new Vector3(-5, 0, i), new Vector3(5, 0, i) }, new Color(.26f, .29f, .34f));
            }
            Circle(target + Vector3.up * .02f, .3f, 0, new Color(1, .65f, .22f));
            Line(new[] { target, target + Vector3.up * 1.7f }, new Color(1, .65f, .22f));
            if (shapes && skill is RangedSkill range)
            {
                Circle(center + Vector3.up * .01f, range.ServerRadius, 0, new Color(.35f, .65f, .95f));
                foreach (var shape in range.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>()) Shape(shape, center);
            }
            else if (shapes && skill.UsesRO && skill.RangeDetector != null)
            {
                foreach (var shape in skill.RangeDetector.Shapes ?? Array.Empty<SkillRangeDetector.Shape>()) Shape(shape, center);
            }
            dirty = false;
        }
        static Vector3 Center(RangedSkill skill, Vector3 target)
        {
            Vector3 center = skill.SpawnPoint == ROSpawnPoint.Target ? target : Vector3.zero;
            return center + skill.Offset;
        }
        void AddEffect(GameObject prefab, float time, Vector3 position)
        {
            if (prefab == null) return;
            var effect = Clone(prefab); effect.transform.position = position;
            effects.Add(effect); effectTimes.Add(time);
        }
        void Sample(SkillDefinition skill, float time)
        {
            if (graph.IsValid())
            {
                float duration = Mathf.Max(.001f, skill.AttackDuration);
                motion.SetTime(Math.Min(time / duration * skill.Animation.length, Math.Max(0, skill.Animation.length - .00001f)));
                graph.Evaluate(0);
                animator.transform.localPosition = motionPosition; animator.transform.localRotation = motionRotation;
            }
            if (ro != null) Simulate(ro, time - (skill is RangedSkill ranged ? ranged.SpawnTime : 0));
            for (int i = 0; i < effects.Count; ++i) Simulate(effects[i], time - effectTimes[i]);
        }
        static void Simulate(GameObject obj, float elapsed)
        {
            obj.SetActive(elapsed >= 0);
            if (elapsed < 0) return;
            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
            {
                // 각 파티클을 한 번씩 결정적으로 샘플링한다.
                ps.Simulate(elapsed, false, true, true);
            }
        }
        void Shape(SkillRangeDetector.Shape shape, Vector3 center)
        {
            Vector3 origin = center + shape.Origin;
            Color color = new Color(.2f, .9f, .9f);
            for (int axis = 0; axis < 3; ++axis) Circle(origin, shape.Radius, axis, color);
        }
        void Circle(Vector3 center, float radius, int axis, Color color)
        {
            var points = new Vector3[49];
            for (int i = 0; i < points.Length; ++i)
            {
                float a = i * Mathf.PI * 2 / (points.Length - 1), x = Mathf.Cos(a) * radius, y = Mathf.Sin(a) * radius;
                points[i] = center + (axis == 0 ? new Vector3(x, 0, y) : axis == 1 ? new Vector3(x, y, 0) : new Vector3(0, x, y));
            }
            Line(points, color);
        }
        void Line(Vector3[] points, Color color)
        {
            var obj = new GameObject("Guide") { hideFlags = HideFlags.HideAndDontSave };
            obj.transform.SetParent(guides.transform, false);
            var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial; line.useWorldSpace = false;
            line.startColor = line.endColor = color; line.startWidth = line.endWidth = .015f;
            line.positionCount = points.Length; line.SetPositions(points);
        }
        void ClearObjects()
        {
            if (graph.IsValid()) graph.Destroy();
            if (character != null) Object.DestroyImmediate(character);
            if (ro != null) Object.DestroyImmediate(ro);
            if (guides != null) Object.DestroyImmediate(guides);
            foreach (var effect in effects) if (effect != null) Object.DestroyImmediate(effect);
            effects.Clear(); effectTimes.Clear(); animator = null;
        }
        public void Dispose()
        {
            ClearObjects(); utility?.Cleanup(); utility = null;
            if (lineMaterial != null) Object.DestroyImmediate(lineMaterial);
        }
    }
}
