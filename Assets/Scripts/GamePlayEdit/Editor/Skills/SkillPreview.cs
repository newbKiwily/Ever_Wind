using System;
using EverWind.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace EverWind.SkillEditing
{
    // 렌더링과 탐지용 객체를 편집 중인 맵에서 분리한다.
    public sealed class SkillPreview : IDisposable
    {
        readonly PreviewRenderUtility preview;
        readonly Scene physicsScene;
        readonly GameObject root, model, target, physicsTarget;
        readonly SkillRangeDetector detector;
        GameObject cast, effect, ro;
        GameObject castSource, effectSource, roSource;
        readonly Animator animator;
        readonly Vector3 modelPosition;
        readonly Quaternion modelRotation;
        Vector2 orbit = new Vector2(145, 15);
        float distance = 7;
        PlayableGraph graph;
        AnimationClipPlayable motion;
        AnimationClip clip;
        bool disposed;
        Vector2 viewSize;
        public Vector3 TargetPosition = new Vector3(0, .75f, 2);
        public bool Detected { get; private set; }

        public SkillPreview()
        {
            preview = new PreviewRenderUtility();
            try
            {
            preview.camera.fieldOfView = 35;
            preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 40, 0);
            preview.lights[1].intensity = .8f;
            root = new GameObject("Skill preview"); preview.AddSingleGO(root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerVisual.prefab");
            if (prefab != null)
            {
                // 게임플레이 스크립트가 없는 기존 Visual만 사용한다.
                model = Object.Instantiate(prefab, root.transform);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                animator = model.GetComponentInChildren<Animator>();
                if (animator != null) { animator.runtimeAnimatorController = null; animator.applyRootMotion = false; animator.fireEvents = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
                modelPosition = model.transform.localPosition; modelRotation = model.transform.localRotation;
            }
            target = GameObject.CreatePrimitive(PrimitiveType.Capsule); target.name = "미리보기 몬스터";
            SceneManager.MoveGameObjectToScene(target, root.scene); target.transform.SetParent(root.transform, false);
            target.transform.localScale = new Vector3(.6f, .75f, .6f); Object.DestroyImmediate(target.GetComponent<Collider>());
            physicsScene = EditorSceneManager.NewPreviewScene();
            if (!physicsScene.GetPhysicsScene().IsValid() || physicsScene.GetPhysicsScene() == Physics.defaultPhysicsScene)
                throw new InvalidOperationException("독립된 미리보기 물리 공간을 준비하지 못했습니다.");
            physicsTarget = new GameObject("Skill preview collider") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(physicsTarget, physicsScene);
            var capsule = physicsTarget.AddComponent<CapsuleCollider>(); capsule.radius = .3f; capsule.height = 1.5f;
            var query = new GameObject("Skill preview query") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(query, physicsScene); detector = query.AddComponent<SkillRangeDetector>();
            }
            catch { Dispose(); throw; }
        }

        public void Evaluate(SkillDefinition skill, float time)
        {
            if (skill == null) return;
            target.transform.position = TargetPosition; physicsTarget.transform.position = TargetPosition;
            if (model != null && animator != null && skill.Animation != null)
            {
                if (clip != skill.Animation)
                {
                    if (graph.IsValid()) graph.Destroy();
                    clip = skill.Animation;
                    graph = PlayableGraph.Create("EverWind skill editor"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    motion = AnimationClipPlayable.Create(graph, clip); motion.SetApplyFootIK(false); motion.SetApplyPlayableIK(false);
                    AnimationPlayableOutput.Create(graph, "Character", animator).SetSourcePlayable(motion); graph.Play();
                }
                motion.SetTime(Mathf.Clamp01(time / Mathf.Max(.001f, skill.AttackDuration)) * Mathf.Max(0, clip.length - .00001f));
                graph.Evaluate(0);
                model.transform.localPosition = modelPosition; model.transform.localRotation = modelRotation;
            }
            else if (graph.IsValid()) { graph.Destroy(); clip = null; }
            var r = skill as RangedSkill;
            Vector3 center = r != null ? r.Center(root.transform, target.transform) : Vector3.zero;
            UpdateEffect(ref cast, ref castSource, skill.CastEffect, Vector3.up + root.transform.TransformDirection(skill.CastEffectOffset), time - skill.CastEffectTime, true, rotation: skill.CastEffectRotation);
            if (cast != null && time >= skill.AttackDuration) cast.SetActive(false);
            UpdateEffect(ref effect, ref effectSource, skill.SwingEffect, (r != null ? center : Vector3.up) + root.transform.TransformDirection(skill.SwingEffectOffset), time - skill.EffectDelay, rotation: skill.SwingEffectRotation);
            UpdateEffect(ref ro, ref roSource, r != null ? r.ROModel : null, center + (r != null && r.ROModel != null ? root.transform.TransformDirection(r.ROModelOffset) : Vector3.zero), time - (r != null ? r.SpawnTime : 0), rotation: r != null && r.ROModel != null ? r.ROModelRotation : Vector3.zero, empty: r != null);
            detector.transform.SetPositionAndRotation(center, Quaternion.identity);
            detector.Shapes = r != null ? r.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>() : Array.Empty<SkillRangeDetector.Shape>();
            Physics.SyncTransforms();
            Detected = false;
            if (r != null && time >= r.SpawnTime)
                foreach (var collider in detector.DetectColliders(physicsScene.GetPhysicsScene())) if (collider.gameObject == physicsTarget) Detected = true;
        }

        void UpdateEffect(ref GameObject instance, ref GameObject source, GameObject prefab, Vector3 position, float elapsed, bool followCaster = false, Vector3 rotation = default, bool empty = false)
        {
            if (source != prefab || (instance == null && empty) || (instance != null && prefab == null && !empty))
            {
                if (instance != null) Object.DestroyImmediate(instance);
                source = prefab; instance = prefab != null ? Object.Instantiate(prefab, root.transform) : empty ? new GameObject("RO") : null;
                if (instance != null)
                {
                    instance.transform.SetParent(root.transform, false);
                    foreach (var c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                    foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ps.useAutoRandomSeed = false; ps.randomSeed = 1;
                        var main = ps.main; main.stopAction = ParticleSystemStopAction.None; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                        if (followCaster) main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    }
                }
            }
            if (instance == null) return;
            instance.transform.SetPositionAndRotation(position, root.transform.rotation * Quaternion.Euler(rotation)); instance.SetActive(elapsed >= 0);
            if (elapsed < 0) return;
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true)) ps.Simulate(elapsed, false, true, false);
        }



        public void Draw(Rect rect, SkillDefinition skill)
        {
            if (rect.width < 2 || rect.height < 2) return;

            preview.camera.transform.rotation = Quaternion.Euler(orbit.y, orbit.x, 0);
            preview.camera.transform.position = new Vector3(0, .8f, 1) - preview.camera.transform.forward * distance;
            preview.camera.nearClipPlane = .05f; preview.camera.farClipPlane = 100;
            if (Event.current.type == EventType.Repaint)
            {
                preview.BeginPreview(rect, GUIStyle.none);
                Texture image;
                try { preview.Render(true); }
                finally { image = preview.EndPreview(); }
                GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, false);
            }
            viewSize = rect.size;
            GUI.BeginGroup(rect);
            try
            {
            if (skill is RangedSkill ranged && ranged.DetectionShapes != null)
            {
                    for (int i = 0; i < ranged.DetectionShapes.Length; ++i)
                    {
                        var shape = ranged.DetectionShapes[i];
                        WireSphere(shape.Origin, shape.Radius, Color.cyan);
                    }
                WireSphere(Vector3.zero, ranged.ServerRadius, new Color(1, .65f, .2f));
            }
            }
            finally { GUI.EndGroup(); }
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, 22), "RO 탐지: " + (Detected ? "대상 포함" : "대상 없음") + "  |  우클릭 회전 / 휠 확대", EditorStyles.whiteLabel);
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == 1) { orbit.x += e.delta.x; orbit.y = Mathf.Clamp(orbit.y + e.delta.y, -80, 80); e.Use(); }
                if (e.type == EventType.ScrollWheel) { distance = Mathf.Clamp(distance + e.delta.y * .2f, 1.5f, 30); e.Use(); }
            }
        }

        Vector2 Project(Vector3 local)
        {
            Vector3 p = preview.camera.WorldToViewportPoint(detector.transform.TransformPoint(local));
            return new Vector2(p.x * viewSize.x, (1 - p.y) * viewSize.y);
        }
        void WorldLine(Vector3 a, Vector3 b, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Vector2 from = Project(a), to = Project(b), delta = to - from;
            // 회전한 GUI 선이 왼쪽 입력 영역으로 넘어가지 않도록 끝점을 먼저 자른다.
            float first = 0, last = 1;
            bool Clip(float p, float q)
            {
                if (Mathf.Abs(p) < .00001f) return q >= 0;
                float t = q / p;
                if (p < 0) first = Mathf.Max(first, t); else last = Mathf.Min(last, t);
                return first <= last;
            }
            if (!Clip(-delta.x, from.x - 2) || !Clip(delta.x, viewSize.x - 2 - from.x)
                || !Clip(-delta.y, from.y - 2) || !Clip(delta.y, viewSize.y - 2 - from.y)) return;
            to = from + delta * last; from += delta * first; delta = to - from;
            var matrix = GUI.matrix;
            try
            {
                GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
                EditorGUI.DrawRect(new Rect(from.x, from.y, delta.magnitude, 1.5f), color);
            }
            finally { GUI.matrix = matrix; }
        }
        void WireSphere(Vector3 p, float radius, Color color)
        {
            if (Event.current.type != EventType.Repaint || !float.IsFinite(radius)) return;
            for (int axis = 0; axis < 3; ++axis)
            for (int i = 0; i < 48; ++i)
            {
                Vector3 Point(int index)
                {
                    float a = index * Mathf.PI / 24, x = Mathf.Cos(a) * radius, y = Mathf.Sin(a) * radius;
                    return p + (axis == 0 ? new Vector3(x, y, 0) : axis == 1 ? new Vector3(x, 0, y) : new Vector3(0, x, y));
                }
                WorldLine(Point(i), Point(i + 1), color);
            }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (graph.IsValid()) graph.Destroy();
            if (physicsScene.IsValid()) EditorSceneManager.ClosePreviewScene(physicsScene);
            preview?.Cleanup();
        }
    }
}
