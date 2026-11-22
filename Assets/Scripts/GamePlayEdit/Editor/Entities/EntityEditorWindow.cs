using System;
using System.Globalization;
using System.Linq;
using EverWind.Gameplay;
using UnityEditor;
using UnityEngine;

namespace EverWind.EntityEditing
{
    public sealed class EntityEditorWindow : EditorWindow
    {
        [SerializeField] EntityEditorData data = new EntityEditorData();
        [SerializeField] int step;
        [SerializeField] string generatedPath;
        Vector2 scroll;
        string feedback;
        MessageType feedbackType;
        bool busy;
        ActionVisualProfile template;

        [MenuItem("Tools/EverWind/Entity Editor")]
        public static void Open() => GetWindow<EntityEditorWindow>("EverWind 엔티티 편집기");

        void OnEnable()
        {
            minSize = new Vector2(440, 540);
            if (data == null) data = new EntityEditorData();
            if (data.RegistrationScene == null) data.RegistrationScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EntityEditorGenerate.DefaultScenePath);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("엔티티 제작", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. 종류 선택  →  2. 모델 삽입  →  3. 설정 및 생성", EditorStyles.miniLabel);
            if (busy) EditorGUILayout.HelpBox("처리 중입니다. 콘텐츠 빌드가 끝날 때까지 기다려 주세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                using (var view = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = view.scrollPosition;
                    if (step == 0) DrawKind();
                    else if (step == 1) DrawModel();
                    else DrawSettings();
                    if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, feedbackType);
                    if (!string.IsNullOrEmpty(generatedPath))
                    {
                        if (GUILayout.Button("생성 폴더 보기")) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<DefaultAsset>(System.IO.Path.GetDirectoryName(generatedPath).Replace('\\', '/')));
                        if (GUILayout.Button("Addressables 콘텐츠 빌드 다시 실행")) Queue(() =>
                        {
                            EntityEditorGenerate.BuildContent();
                            Message("콘텐츠 빌드 완료. 서버는 다시 빌드해 콘텐츠를 복사한 뒤 재시작하세요.", MessageType.Info);
                        });
                    }
                }
            }
        }

        void DrawKind()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("만들 엔티티의 종류를 선택하세요.");
            int choice = GUILayout.SelectionGrid((int)data.Kind, new[] { "채집물", "플레이어", "몬스터" }, 3, GUILayout.Height(42));
            data.Kind = (EntityEditorKind)choice;
            EditorGUILayout.HelpBox(data.Kind == EntityEditorKind.Player
                ? "플레이어는 현재 공통 외형과 기본 공격력·방어력을 교체합니다. 맵 배치용 프리팹은 만들지 않습니다."
                : "게임용 Visual 프리팹과 맵 에디터에서 드래그할 배치용 프리팹을 생성합니다.", MessageType.Info);
            if (GUILayout.Button("다음: 모델 삽입", GUILayout.Height(30))) Queue(() =>
            {
                var content = EntityContentFile.Read();
                if (data.Kind != EntityEditorKind.Player) data.DefinitionId = content.NextId(data.Kind == EntityEditorKind.Monster ? "monster" : "resource");
                data.VisualId = content.NextVisualId();
                if (data.Kind == EntityEditorKind.Player)
                {
                    var p = content.Rows("player").Single();
                    data.Attack = int.Parse(p[1], CultureInfo.InvariantCulture);
                    data.Defence = int.Parse(p[2], CultureInfo.InvariantCulture);
                }
                step = 1; feedback = null; generatedPath = null;
            });
        }

        void DrawModel()
        {
            EditorGUILayout.Space(10);
            data.Name = EditorGUILayout.TextField("생성 이름", data.Name);
            data.Model = (GameObject)EditorGUILayout.ObjectField("모델 / 외형 프리팹", data.Model, typeof(GameObject), false);
            EditorGUILayout.HelpBox("Project의 FBX 또는 외형 전용 프리팹을 넣으세요. 게임플레이 스크립트가 포함된 프리팹은 검증에서 거절합니다.", MessageType.Info);
            Rect previewRect = GUILayoutUtility.GetRect(180, 180, GUILayout.ExpandWidth(true));
            if (data.Model != null)
            {
                var image = AssetPreview.GetAssetPreview(data.Model);
                if (image != null) GUI.DrawTexture(previewRect, image, ScaleMode.ScaleToFit);
                else if (AssetPreview.IsLoadingAssetPreview(data.Model.GetInstanceID())) Repaint();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("이전")) Queue(() => { step = 0; feedback = null; });
                using (new EditorGUI.DisabledScope(data.Model == null))
                    if (GUILayout.Button("다음: 종류별 설정")) Queue(() => { step = 2; feedback = null; });
            }
        }

        void DrawSettings()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("종류: " + new[] { "채집물", "플레이어", "몬스터" }[(int)data.Kind], EditorStyles.boldLabel);
            data.Name = EditorGUILayout.TextField("생성 이름", data.Name);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("모델", data.Model, typeof(GameObject), false);
            Section("외형 보정");
            data.VisualOffset = EditorGUILayout.Vector3Field("위치", data.VisualOffset);
            data.VisualEuler = EditorGUILayout.Vector3Field("회전", data.VisualEuler);
            data.VisualScale = EditorGUILayout.FloatField("크기 배율", data.VisualScale);
            if (data.Alive) DrawAnimation();
            Section("서버 기준 데이터");
            if (data.Kind != EntityEditorKind.Player)
            {
                data.DefinitionId = EditorGUILayout.IntField("정의 ID", data.DefinitionId);
                data.Radius = EditorGUILayout.FloatField("서버 반경", data.Radius);
            }
            if (data.Kind == EntityEditorKind.Resource)
            {
                data.ItemId = EditorGUILayout.IntField("지급 아이템 ID", data.ItemId);
                data.Capacity = EditorGUILayout.IntField("채집량", data.Capacity);
                data.GatherTime = EditorGUILayout.FloatField("채집 시간(초)", data.GatherTime);
                data.RefillDelay = EditorGUILayout.FloatField("리필 지연(초)", data.RefillDelay);
                EditorGUILayout.HelpBox("현재 게임은 지급 아이템 ID로 채집물 외형을 찾습니다. 같은 아이템의 모든 채집물이 이 외형을 공유합니다. Animator·전투 Profile은 필요하지 않습니다.", MessageType.Info);
                data.ReplaceResourceVisual = EditorGUILayout.Toggle("기존 공용 채집물 외형 교체", data.ReplaceResourceVisual);
            }
            else
            {
                data.Attack = EditorGUILayout.IntField("기본 공격력", data.Attack);
                data.Defence = EditorGUILayout.IntField("기본 방어력", data.Defence);
                if (data.Kind == EntityEditorKind.Monster)
                {
                    data.VisualId = EditorGUILayout.IntField("클라이언트 외형 ID", data.VisualId);
                    data.MoveSpeed = EditorGUILayout.FloatField("이동 속도", data.MoveSpeed);
                    data.MaxHp = EditorGUILayout.IntField("최대 HP", data.MaxHp);
                    data.Experience = EditorGUILayout.IntField("처치 경험치", data.Experience);
                    data.AttackSkillId = EditorGUILayout.IntField("공격 스킬 ID", data.AttackSkillId);
                    data.AttackCooldown = EditorGUILayout.FloatField("공격 주기(초)", data.AttackCooldown);
                }
                else EditorGUILayout.HelpBox("플레이어 공통 외형과 player 행을 교체합니다. 개인 레벨·장비·성장 저장값과 기존 HP·이동속도는 그대로 사용합니다.", MessageType.Warning);
            }
            if (data.Kind != EntityEditorKind.Player)
            {
                Section("맵 배치 기본값");
                data.AlignRendererBottom = EditorGUILayout.Toggle("모델 바닥을 지면에 맞춤", data.AlignRendererBottom);
                data.HeightOffset = EditorGUILayout.FloatField("배치 높이 보정", data.HeightOffset);
                if (data.Kind == EntityEditorKind.Monster)
                {
                    data.OverrideRespawn = EditorGUILayout.Toggle("개별 리스폰 시간 사용", data.OverrideRespawn);
                    using (new EditorGUI.DisabledScope(!data.OverrideRespawn))
                        data.RespawnSeconds = EditorGUILayout.FloatField("리스폰(초, 0은 비활성)", data.RespawnSeconds);
                }
            }
            Section("출력 및 등록");
            data.RegistrationScene = (SceneAsset)EditorGUILayout.ObjectField("ContainerInMap 등록 씬", data.RegistrationScene, typeof(SceneAsset), false);
            EditorGUILayout.LabelField("생성 폴더", data.Folder, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("Generate는 새 프리팹·외형 참조·서버 정의를 저장하고 Addressables 콘텐츠를 자동 빌드합니다. 생성한 프리팹은 맵 편집기에서 바로 드래그할 수 있으며, 실제 맵 배치는 맵 추출 시 저장됩니다. 서버는 다시 빌드해 콘텐츠를 실행 폴더에 복사한 뒤 재시작하세요.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("이전: 모델")) Queue(() => { step = 1; feedback = null; });
                if (GUILayout.Button("오류 검사")) Queue(() =>
                {
                    EntityEditorGenerate.Validate(data);
                    Message("입력·서버 참조·등록 경로 검사 통과. 파일은 생성하지 않았습니다.", MessageType.Info);
                });
            }
            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(generatedPath)))
                if (GUILayout.Button("Generate · 생성 후 자동 빌드", GUILayout.Height(34))) Queue(Generate);
            if (!string.IsNullOrEmpty(generatedPath) && GUILayout.Button("새 엔티티 만들기")) Queue(() =>
            {
                data = new EntityEditorData { RegistrationScene = data.RegistrationScene };
                step = 0; generatedPath = null; feedback = null; template = null;
            });
        }

        void DrawAnimation()
        {
            Section("Animator · ActionVisualProfile");
            template = (ActionVisualProfile)EditorGUILayout.ObjectField("기존 Profile에서 복사(선택)", template, typeof(ActionVisualProfile), false);
            using (new EditorGUI.DisabledScope(template == null))
                if (GUILayout.Button("Profile의 클립·연출값 가져오기")) Queue(CopyProfile);
            data.Avatar = (Avatar)EditorGUILayout.ObjectField("Avatar(생략: 모델 설정)", data.Avatar, typeof(Avatar), false);
            data.Idle = Clip("대기 *", data.Idle);
            data.Run = Clip("이동 *", data.Run);
            data.CombatIdle = Clip("전투 대기(생략: 대기)", data.CombatIdle);
            data.CombatRun = Clip("전투 이동(생략: 이동)", data.CombatRun);
            if (data.Skills == null || data.Skills.Length != 5) Array.Resize(ref data.Skills, 5);
            int count = data.Kind == EntityEditorKind.Player ? 5 : 1;
            for (int i = 0; i < count; ++i) data.Skills[i] = Clip(i == 0 ? "기본 공격 / Skill1 *" : "Skill" + (i + 1) + "(생략: 기본 공격)", data.Skills[i]);
            if (data.Kind == EntityEditorKind.Player)
            {
                data.Gather = Clip("채집 *", data.Gather);
                data.Craft = Clip("제작(생략: 채집)", data.Craft);
                data.Success = Clip("성공(생략: 대기)", data.Success);
            }
            data.Hit = Clip("피격 *", data.Hit);
            data.Knockback = Clip("넉백(생략: 피격)", data.Knockback);
            data.Dead = Clip("사망 *", data.Dead);
            data.DeadHeightOffset = EditorGUILayout.FloatField("사망 높이 보정", data.DeadHeightOffset);
            data.SwingEffect = (GameObject)EditorGUILayout.ObjectField("공격 이펙트", data.SwingEffect, typeof(GameObject), false);
            data.HitEffect = (GameObject)EditorGUILayout.ObjectField("피격 이펙트", data.HitEffect, typeof(GameObject), false);
            data.EffectOffset = EditorGUILayout.Vector3Field("이펙트 위치 보정", data.EffectOffset);
            data.SwingCue = EditorGUILayout.FloatField("공격 이펙트 지연(초)", data.SwingCue);
            data.AttackImpactNormalized = EditorGUILayout.Slider("타격 표현 비율", data.AttackImpactNormalized, .05f, .95f);
            EditorGUILayout.HelpBox("* 표시는 필수입니다. 기존 공통 Animator에 클립을 연결합니다. Root Motion과 애니메이션 이벤트는 끄며, 실제 피해·채집 판정은 서버가 유지합니다. Generic 클립은 모델과 같은 리그를 사용하세요.", MessageType.Info);
        }

        void CopyProfile()
        {
            data.Idle = template.ResolveClip("Idle"); data.Run = template.ResolveClip("Run");
            data.CombatIdle = template.ResolveClip("CombatIdle"); data.CombatRun = template.ResolveClip("CombatRun");
            data.Gather = template.ResolveClip("Gather"); data.Craft = template.ResolveClip("Craft");
            data.Hit = template.ResolveClip("Hit"); data.Knockback = template.ResolveClip("Knockback");
            data.Dead = template.ResolveClip("Dead"); data.Success = template.ResolveClip("Success");
            data.Skills = Enumerable.Range(1, 5).Select(i => template.ResolveClip("Skill" + i)).ToArray();
            data.VisualOffset = template.VisualOffset; data.VisualEuler = template.VisualEuler; data.VisualScale = template.VisualScale;
            data.DeadHeightOffset = template.DeadHeightOffset;
            data.SwingEffect = template.SwingEffect; data.HitEffect = template.HitEffect;
            data.EffectOffset = template.EffectOffset; data.SwingCue = template.SwingCue; data.AttackImpactNormalized = template.AttackImpactNormalized;
            Message("Profile 설정을 가져왔습니다. 모델에 맞는 클립·Avatar인지 확인하세요.", MessageType.Info);
        }

        void Generate()
        {
            generatedPath = EntityEditorGenerate.Generate(data);
            try
            {
                EntityEditorGenerate.BuildContent();
                Message("생성·등록·콘텐츠 빌드 완료: " + generatedPath, MessageType.Info);
            }
            catch (Exception e)
            {
                Message("생성·등록은 완료했지만 콘텐츠 빌드에 실패했습니다. 원인을 해결한 뒤 빌드 다시 실행을 누르세요.\n" + e.Message, MessageType.Error);
            }
        }

        void Queue(Action action)
        {
            busy = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                try { action(); }
                catch (Exception e) { Message(e.Message, MessageType.Error); }
                finally { busy = false; Repaint(); }
            };
        }

        void Message(string text, MessageType type) { feedback = text; feedbackType = type; }
        static AnimationClip Clip(string label, AnimationClip value) => (AnimationClip)EditorGUILayout.ObjectField(label, value, typeof(AnimationClip), false);
        static void Section(string title) { EditorGUILayout.Space(10); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }
    }
}
