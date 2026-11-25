using System;
using System.Globalization;
using System.IO;
using System.Linq;
using EverWind.EntityEditing;
using EverWind.Gameplay;
using UnityEditor;
using UnityEngine;

namespace EverWind.SkillEditing
{
    public sealed class SkillEditorWindow : EditorWindow
    {
        [SerializeField] SkillEditorData data = new SkillEditorData();
        [SerializeField] SkillDefinition selected;
        [SerializeField] int type;
        [SerializeField] bool autoBuild = true;
        [SerializeField] Vector3 previewTarget = new Vector3(0, .75f, 2);
        SkillPreview preview;
        Vector2 scroll;
        float time, speed = 1;
        bool playing, busy;
        double updated;
        string feedback;
        string previewError;
        MessageType feedbackType;
        Action pending;

        [MenuItem("Tools/EverWind/Skill Editor")]
        public static void Open() => GetWindow<SkillEditorWindow>("EverWind 스킬");
        void OnEnable()
        {
            minSize = new Vector2(780, 680);
            busy = false; playing = false; previewError = null;
            if (data == null) data = new SkillEditorData();
            if (data.Catalog == null) data.Catalog = AssetDatabase.LoadAssetAtPath<GameplayCatalog>(SkillEditorGenerate.CatalogPath);
            EditorApplication.update += UpdatePreview; EditorApplication.playModeStateChanged += PlayModeChanged;
            updated = EditorApplication.timeSinceStartup;
        }
        void OnDisable() { EditorApplication.update -= UpdatePreview; EditorApplication.playModeStateChanged -= PlayModeChanged; pending = null; busy = false; ReleasePreview(); }
        void OnDestroy() { Discard(); }
        void PlayModeChanged(PlayModeStateChange _) { playing = false; ReleasePreview(); }
        void ReleasePreview() { preview?.Dispose(); preview = null; previewError = null; }
        void Discard() { if (data?.Draft != null && !EditorUtility.IsPersistent(data.Draft)) DestroyImmediate(data.Draft); }
        void UpdatePreview()
        {
            if (pending != null)
            {
                var action = pending; pending = null;
                try { action(); }
                catch (Exception e) { feedback = e.Message; feedbackType = MessageType.Error; }
                finally { busy = false; Repaint(); }
            }
            double now = EditorApplication.timeSinceStartup;
            if (playing && data.Draft != null)
            {
                time += (float)Math.Min(now - updated, .1) * speed;
                if (time >= data.Draft.AttackDuration) { time = data.Draft.AttackDuration; playing = false; }
            }
            updated = now;
            if (data.Draft != null && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) Repaint();
        }
        void Run(Action action)
        {
            if (busy) return;
            pending = action; busy = true;
        }
        void New()
        {
            var next = SkillEditorGenerate.New(data.Catalog, type == 0 ? typeof(SingleTargetSkill) : type == 1 ? typeof(RangedSkill) : typeof(BuffSkill));
            ReleasePreview(); Discard(); data = next;
            data.AssetName = "Skill" + data.Draft.Id;
            if (data.Draft is RangedSkill ranged) { ranged.RequiresTarget = true; ranged.DetectionShapes = Array.Empty<SkillRangeDetector.Shape>(); }
            time = 0; playing = false; feedback = null;
        }
        void Load()
        {
            if (selected == null) return;
            var next = SkillEditorGenerate.Load(data.Catalog, selected);
            ReleasePreview(); Discard(); data = next;
            time = 0; playing = false;
            feedback = null;
        }

        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                data.Catalog = (GameplayCatalog)EditorGUILayout.ObjectField("GameplayCatalog", data.Catalog, typeof(GameplayCatalog), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    type = EditorGUILayout.Popup("새 스킬 종류", type, new[] { "Single — 락온 공격", "Ranged — RO 생성", "Buff — 준비만" });
                    if (GUILayout.Button("+ 스킬 추가", GUILayout.Width(110))) Run(New);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    selected = (SkillDefinition)EditorGUILayout.ObjectField("기존 SO", selected, typeof(SkillDefinition), false);
                    using (new EditorGUI.DisabledScope(selected == null)) if (GUILayout.Button("불러오기", GUILayout.Width(110))) Run(Load);
                }
                if (data.Draft == null) { EditorGUILayout.HelpBox("종류를 선택하고 스킬을 추가하세요. Generate 전까지 원본 에셋과 서버 파일은 수정하지 않습니다.", MessageType.Info); return; }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var view = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Width(Mathf.Clamp(position.width * .44f, 340, 520))))
                    { scroll = view.scrollPosition; DrawFields(); }
                    using (new EditorGUILayout.VerticalScope()) DrawPreview();
                }
            }
            if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, feedbackType);
            if (busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                EditorGUILayout.HelpBox(busy ? "요청을 처리 중입니다." : "편집 모드에서 컴파일이 끝난 뒤 수정할 수 있습니다.", MessageType.Info);
        }

        void DrawFields()
        {
            var s = data.Draft;
            EditorGUILayout.LabelField(s.GetType().Name, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(data.Source != null))
            { data.AssetName = EditorGUILayout.TextField("SO 이름", data.AssetName); s.Id = EditorGUILayout.IntField("서버 ID", s.Id); }
            if (data.Source != null) EditorGUILayout.HelpBox("등록된 스킬은 이름·ID만 고정됩니다. 아래 수치는 수정 후 Generate로 반영하세요.", MessageType.None);
            var clip = (AnimationClip)EditorGUILayout.ObjectField("애니메이션", s.Animation, typeof(AnimationClip), false);
            if (clip != s.Animation)
            {
                s.Animation = clip;
                if (clip != null) ResizeDuration(clip.length);
            }
            float duration = EditorGUILayout.FloatField("공격 시간(초)", s.AttackDuration);
            if (duration != s.AttackDuration && duration > 0 && EntityContentFile.Finite(duration)) ResizeDuration(duration);
            if (s.Animation != null) EditorGUILayout.LabelField("재생 속도", (s.Animation.length / Mathf.Max(.001f, s.AttackDuration)).ToString("0.###") + " 배속");
            bool buff = s is BuffSkill;
            using (new EditorGUI.DisabledScope(buff))
            { s.Reach = EditorGUILayout.FloatField("사거리", s.Reach); s.Damage = EditorGUILayout.FloatField("타격당 기본 대미지", s.Damage); }
            if (buff) { s.Reach = s.Damage = 0; EditorGUILayout.HelpBox("버프 작성 틀입니다. 실제 효과와 저장·등록은 아직 지원하지 않습니다.", MessageType.Info); }
            s.Cooldown = EditorGUILayout.FloatField("기본 쿨타임", s.Cooldown);
            s.AnimationSlot = EditorGUILayout.IntSlider("Animator 슬롯", s.AnimationSlot, 1, 5);
            EditorGUILayout.LabelField(buff ? "효과 예정 시각(초)" : "공격 타이밍(초)", EditorStyles.boldLabel);
            var times = s.AttackTimes ?? Array.Empty<float>();
            for (int i = 0; i < times.Length; ++i)
            {
                int index = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    times[i] = EditorGUILayout.FloatField((i + 1).ToString(), times[i]);
                    if (GUILayout.Button("현재", GUILayout.Width(45))) times[i] = time;
                    if (GUILayout.Button("−", GUILayout.Width(25))) Run(() => s.AttackTimes = s.AttackTimes.Where((_, n) => n != index).ToArray());
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 현재 시각 추가")) Run(() => s.AttackTimes = times.Concat(new[] { time }).ToArray());
                if (GUILayout.Button("시각 정렬")) Run(() => Array.Sort(s.AttackTimes));
            }
            s.CastEffect = (GameObject)EditorGUILayout.ObjectField("시전 파티클", s.CastEffect, typeof(GameObject), false);
            s.CastEffectTime = EditorGUILayout.FloatField("시전 파티클 시각", s.CastEffectTime);
            s.CastEffectOffset = EditorGUILayout.Vector3Field("시전 파티클 오프셋 XYZ", s.CastEffectOffset);
            s.CastEffectRotation = EditorGUILayout.Vector3Field("시전 파티클 회전 XYZ", s.CastEffectRotation);
            s.SwingEffect = (GameObject)EditorGUILayout.ObjectField("스킬 파티클", s.SwingEffect, typeof(GameObject), false);
            s.EffectDelay = EditorGUILayout.FloatField("스킬 파티클 시각", s.EffectDelay);
            s.SwingEffectOffset = EditorGUILayout.Vector3Field("스킬 파티클 오프셋 XYZ", s.SwingEffectOffset);
            s.SwingEffectRotation = EditorGUILayout.Vector3Field("스킬 파티클 회전 XYZ", s.SwingEffectRotation);
            if (s is RangedSkill ranged) DrawRanged(ranged);
            if (s.UsesRO && !(s is RangedSkill))
            {
                s.RangeDetector = (SkillRangeDetector)EditorGUILayout.ObjectField("기존 탐지 프리팹", s.RangeDetector, typeof(SkillRangeDetector), false);
                data.LegacyAreaRadius = EditorGUILayout.FloatField("기존 서버 범위", data.LegacyAreaRadius);
            }
            data.Knockback = EditorGUILayout.FloatField("넉백 거리", data.Knockback);
            data.DamageRatio = EditorGUILayout.FloatField("공격력 배율(-1: 고정 피해)", data.DamageRatio);
            EditorGUILayout.Space(); EditorGUILayout.LabelField("서버 랭크별 배율 / 쿨타임", EditorStyles.boldLabel);
            for (int i = 0; i < 5; ++i)
                using (new EditorGUILayout.HorizontalScope())
                { EditorGUILayout.LabelField("랭크 " + (i + 1), GUILayout.Width(52)); data.Multipliers[i] = EditorGUILayout.FloatField(data.Multipliers[i]); data.Cooldowns[i] = EditorGUILayout.FloatField(data.Cooldowns[i]); }
            EditorGUILayout.HelpBox("대미지는 타격당 기본값 × 랭크 배율에 서버 방어력을 적용합니다. 기존 스킬의 공격력 계산은 유지됩니다. 새 스킬의 습득/단축키 지정은 기존 진행 시스템을 따릅니다.", MessageType.None);
            EditorGUILayout.LabelField("SO 출력", data.Path, EditorStyles.wordWrappedMiniLabel);
            autoBuild = EditorGUILayout.Toggle("생성 후 콘텐츠 자동 빌드", autoBuild);
            using (new EditorGUI.DisabledScope(buff))
            {
                if (GUILayout.Button("오류 검사")) Run(() => { SkillEditorGenerate.Validate(data); feedback = "입력과 연결 검사 통과. 에셋/서버 데이터는 수정하지 않았습니다."; feedbackType = MessageType.Info; });
                if (GUILayout.Button("Generate — 저장 및 등록", GUILayout.Height(30))) Run(Generate);
            }
        }

        void DrawRanged(RangedSkill r)
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("RO 생성과 탐지", EditorStyles.boldLabel);
            r.ROModel = (GameObject)EditorGUILayout.ObjectField("RO 시각 모델/파티클", r.ROModel, typeof(GameObject), false);
            r.ROModelOffset = EditorGUILayout.Vector3Field("RO 시각 오프셋 XYZ", r.ROModelOffset);
            r.ROModelRotation = EditorGUILayout.Vector3Field("RO 시각 회전 XYZ", r.ROModelRotation);
            r.SpawnPoint = EditorGUILayout.Popup("RO 생성 기준", r.SpawnPoint == ROSpawnPoint.Caster ? 1 : 0, new[] { "몬스터 기준", "플레이어 기준" }) == 0 ? ROSpawnPoint.Target : ROSpawnPoint.Caster;
            r.RequiresTarget = r.NeedsTarget;
            EditorGUILayout.HelpBox("선택한 기준의 발동 위치에 생성합니다. None이면 빈 GameObject 하나에 통합 검사기를 넣습니다. 생성 후에는 기준의 이동을 따라가지 않습니다.", MessageType.None);
            r.Offset = EditorGUILayout.Vector3Field("생성 위치 오프셋", r.Offset);
            r.SpawnTime = EditorGUILayout.FloatField("RO 생성 시각", r.SpawnTime);
            r.ServerRadius = EditorGUILayout.FloatField("서버 검증 반경(주황색)", r.ServerRadius);
            DrawShapes(r);
            if (GUILayout.Button("배치한 검사 범위로 서버 반경 맞춤")) FitRadius(r);
            EditorGUILayout.HelpBox("OverlapSphere의 XYZ 중심 좌표와 반경을 직접 입력하세요. 여러 구를 하나의 RO 검사 컴포넌트로 저장하며, 겹친 대상은 한 번만 반환합니다.", MessageType.None);
        }
        void DrawShapes(RangedSkill r)
        {
            EditorGUILayout.LabelField("통합 검사 오브젝트의 구성", EditorStyles.boldLabel);
            if (GUILayout.Button("+ OverlapSphere 추가"))
                Run(() => AddShape(new SkillRangeDetector.Shape { Radius = .5f }));
            var shapes = r.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>();
            for (int i = 0; i < shapes.Length; ++i)
            {
                int index = i;
                var shape = shapes[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField((i + 1) + " · " + "OverlapSphere", EditorStyles.boldLabel);
                        if (GUILayout.Button("복제", GUILayout.Width(44))) Run(() => AddShape(r.DetectionShapes[index]));
                        if (GUILayout.Button("삭제", GUILayout.Width(44))) Run(() => { Undo.RecordObject(r, "검사 범위 삭제"); r.DetectionShapes = r.DetectionShapes.Where((_, n) => n != index).ToArray(); });
                    }
                    EditorGUI.BeginChangeCheck();
                    shape.Origin = EditorGUILayout.Vector3Field("중심 좌표 (RO 기준 XYZ)", shape.Origin);
                    shape.Radius = EditorGUILayout.FloatField("반경", shape.Radius);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(r, "검사 구 수정"); r.DetectionShapes[index] = shape; EditorUtility.SetDirty(r); }
                }
            }
        }
        void AddShape(SkillRangeDetector.Shape shape)
        {
            if (!(data.Draft is RangedSkill ranged)) return;
            Undo.RecordObject(ranged, "검사 구 추가");
            ranged.DetectionShapes = (ranged.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>()).Append(shape).ToArray();
            EditorUtility.SetDirty(ranged);
        }
        static void FitRadius(RangedSkill ranged)
        {
            float extent = .01f;
            foreach (var shape in ranged.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>())
            {
                extent = Mathf.Max(extent, shape.Origin.magnitude + shape.Radius);
            }
            Undo.RecordObject(ranged, "서버 검증 반경 맞춤"); ranged.ServerRadius = extent; EditorUtility.SetDirty(ranged);
        }
        void ResizeDuration(float duration)
        {
            var s = data.Draft; float factor = duration / Mathf.Max(.001f, s.AttackDuration);
            if (s.AttackTimes != null) for (int i = 0; i < s.AttackTimes.Length; ++i) s.AttackTimes[i] *= factor;
            s.CastEffectTime *= factor; s.EffectDelay *= factor;
            if (s is RangedSkill r) r.SpawnTime *= factor;
            time *= factor; s.AttackDuration = duration;
        }
        void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(playing ? "멈춤" : "재생")) { if (time >= data.Draft.AttackDuration) time = 0; playing = !playing; }
                if (GUILayout.Button("처음")) { time = 0; playing = false; }
                speed = EditorGUILayout.Slider("미리보기 속도", speed, .05f, 1);
            }
            time = EditorGUILayout.Slider("시간(초)", time, 0, Mathf.Max(.001f, data.Draft.AttackDuration));
            Rect timeline = GUILayoutUtility.GetRect(10, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(timeline, new Color(.14f, .14f, .14f));
            foreach (float t in data.Draft.AttackTimes ?? Array.Empty<float>())
            {
                float x = timeline.x + Mathf.Clamp01(t / Mathf.Max(.001f, data.Draft.AttackDuration)) * timeline.width;
                EditorGUI.DrawRect(new Rect(x, timeline.y, 2, timeline.height), Color.red);
            }
            float cursor = timeline.x + time / Mathf.Max(.001f, data.Draft.AttackDuration) * timeline.width;
            EditorGUI.DrawRect(new Rect(cursor, timeline.y, 2, timeline.height), Color.white);
            EditorGUILayout.LabelField("빨강: 타격 시점 / 흰색: 현재 시각", EditorStyles.miniLabel);
            previewTarget = new Vector3(0, .75f, EditorGUILayout.FloatField("미리보기 대상 Z", previewTarget.z));
            Rect rect = GUILayoutUtility.GetRect(100, 300, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (!string.IsNullOrEmpty(previewError))
            {
                EditorGUI.HelpBox(rect, "미리보기 오류: " + previewError + "\n수치는 계속 편집할 수 있습니다.", MessageType.Warning);
                if (GUILayout.Button("미리보기 다시 시작")) ReleasePreview();
                return;
            }
            try
            {
                if (preview == null) preview = new SkillPreview();
                preview.TargetPosition = previewTarget;
                preview.Evaluate(data.Draft, time);
                preview.Draw(rect, data.Draft);

            }
            catch (Exception e) when (!(e is ExitGUIException))
            { preview?.Dispose(); preview = null; playing = false; previewError = e.GetType().Name + ": " + e.Message; }
        }
        void Generate()
        {
            busy = true; playing = false; ReleasePreview();
            try
            {
                string path = SkillEditorGenerate.Generate(data);
                selected = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path); Load();
                feedback = "SO · RO 프리팹 · 카탈로그 · 서버 GameData.ewdata 저장 완료."; feedbackType = MessageType.Info;
                if (autoBuild)
                {
                    try { EntityEditorGenerate.BuildContent(); feedback += " 콘텐츠 빌드 완료."; }
                    catch (Exception e) { feedback += " 저장은 완료했지만 빌드 실패: " + e.Message; feedbackType = MessageType.Warning; }
                }
                EditorGUIUtility.PingObject(selected);
            }
            finally { busy = false; }
        }
    }
}
