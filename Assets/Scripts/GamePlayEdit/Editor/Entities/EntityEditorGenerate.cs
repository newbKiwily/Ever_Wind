using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EverWind.Gameplay;
using EverWind.MapEditing;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EverWind.EntityEditing
{
    public static class EntityEditorGenerate
    {
        public const string SharedControllerPath = "Assets/Scripts/GamePlayEdit/Animation/Shared/Alive.controller";
        public const string DefaultScenePath = "Assets/Scenes/LogIn.unity";
        public static readonly string[] States = { "Idle", "Run", "CombatIdle", "CombatRun", "Skill1", "Skill2", "Skill3", "Skill4", "Skill5", "Gather", "Craft", "Hit", "Knockback", "Dead", "Success" };

        public static void Validate(EntityEditorData data)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("컴파일·플레이·빌드 중에는 생성할 수 없습니다.");
            if (data == null || !Enum.IsDefined(typeof(EntityEditorKind), data.Kind)) throw new InvalidOperationException("엔티티 종류를 선택하세요.");
            if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Length > 64 || data.Name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
                throw new InvalidOperationException("이름은 64자 이내의 글자·숫자·밑줄·하이픈으로 입력하세요.");
            if (System.Text.RegularExpressions.Regex.IsMatch(data.Name, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                throw new InvalidOperationException("운영체제 예약 이름은 사용할 수 없습니다.");
            if (Directory.Exists(data.Folder) || File.Exists(data.Folder) || File.Exists(data.Folder + ".meta"))
                throw new InvalidOperationException("같은 이름의 생성 폴더가 있습니다. 새 이름을 입력하세요. 기존 결과는 덮어쓰지 않습니다.");
            if (data.Model == null || !EditorUtility.IsPersistent(data.Model) || !AssetDatabase.IsMainAsset(data.Model))
                throw new InvalidOperationException("Project의 모델 또는 프리팹 루트 에셋을 지정하세요.");
            if (data.Model.GetComponentsInChildren<Renderer>(true).Length == 0) throw new InvalidOperationException("모델에 Renderer가 없습니다.");
            if (data.Model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("게임플레이 스크립트나 누락된 스크립트가 없는 외형 전용 모델/프리팹을 지정하세요.");
            Vector(data.VisualOffset, "외형 위치"); Vector(data.VisualEuler, "외형 회전"); Vector(data.EffectOffset, "이펙트 위치");
            EntityContentFile.Positive(data.VisualScale, "외형 크기");
            if (!EntityContentFile.Finite(data.HeightOffset) || !EntityContentFile.Finite(data.DeadHeightOffset)) throw new InvalidOperationException("높이 보정은 유한한 값이어야 합니다.");
            if (data.Kind == EntityEditorKind.Monster && data.OverrideRespawn) EntityContentFile.NonNegative(data.RespawnSeconds, "리스폰 시간");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.DefaultGroup == null || settings.ActivePlayerDataBuilder == null)
                throw new InvalidOperationException("Addressables 설정·기본 그룹·콘텐츠 빌더를 먼저 지정하세요.");
            if (EditorUtility.IsDirty(settings.DefaultGroup))
                throw new InvalidOperationException("Addressables 기본 그룹의 미저장 변경을 먼저 저장하세요.");
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬의 미저장 변경을 먼저 저장하세요. 자동으로 저장하지 않습니다.");
            EntityContentFile.Read().Validate(data);
            if (data.Alive)
            {
                var shared = AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath);
                if (shared == null || States.Any(s => !shared.animationClips.Any(c => c.name == s)))
                    throw new InvalidOperationException("기존 공통 Alive Animator의 필수 상태 클립이 없습니다.");
                var animators = data.Model.GetComponentsInChildren<Animator>(true);
                if (animators.Length > 1 || data.Model.GetComponentInChildren<Animation>(true) != null)
                    throw new InvalidOperationException("Animator는 최대 하나만 사용하고 Legacy Animation은 제거하세요.");
                if (data.Idle == null || data.Run == null || data.Hit == null || data.Dead == null || data.Skills == null || data.Skills.Length != 5 || data.Skills[0] == null)
                    throw new InvalidOperationException("대기·이동·기본 공격(Skill1)·피격·사망 클립을 지정하세요.");
                if (data.Kind == EntityEditorKind.Player && data.Gather == null) throw new InvalidOperationException("플레이어는 채집 클립도 필요합니다.");
                Avatar avatar = data.Avatar != null ? data.Avatar : animators.FirstOrDefault()?.avatar;
                if (avatar != null && (!EditorUtility.IsPersistent(avatar) || !avatar.isValid)) throw new InvalidOperationException("유효한 Avatar 에셋을 지정하세요.");
                foreach (string state in States)
                {
                    var clip = data.Clip(state);
                    if (clip == null || !EditorUtility.IsPersistent(clip) || clip.legacy || clip.length <= 0)
                        throw new InvalidOperationException("유효한 Mecanim 클립이 필요합니다: " + state);
                    if (clip.humanMotion && (avatar == null || !avatar.isHuman))
                        throw new InvalidOperationException("Humanoid 클립에는 모델에 맞는 Humanoid Avatar가 필요합니다: " + state);
                }
                ValidateEffect(data.SwingEffect); ValidateEffect(data.HitEffect);
                EntityContentFile.NonNegative(data.SwingCue, "공격 이펙트 지연");
                if (!EntityContentFile.Finite(data.AttackImpactNormalized) || data.AttackImpactNormalized < .05f || data.AttackImpactNormalized > .95f)
                    throw new InvalidOperationException("타격 표현 비율은 0.05~0.95여야 합니다.");
            }
            string scenePath = AssetDatabase.GetAssetPath(data.RegistrationScene);
            if (string.IsNullOrEmpty(scenePath)) throw new InvalidOperationException("ContainerInMap이 있는 등록 씬을 지정하세요.");
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try { ValidateRegistration(data, Container(scene)); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static void ValidateEffect(GameObject effect)
        {
            if (effect != null && (!EditorUtility.IsPersistent(effect) || !AssetDatabase.IsMainAsset(effect)))
                throw new InvalidOperationException("이펙트는 Project의 프리팹 에셋을 지정하세요.");
        }

        static void Vector(Vector3 v, string label)
        {
            if (!EntityContentFile.Finite(v.x) || !EntityContentFile.Finite(v.y) || !EntityContentFile.Finite(v.z))
                throw new InvalidOperationException(label + "에 NaN/Infinity를 사용할 수 없습니다.");
        }

        static ContainerInMap Container(Scene scene)
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ContainerInMap>(true)).ToArray();
            if (all.Length != 1) throw new InvalidOperationException("등록 씬에는 ContainerInMap이 정확히 하나 있어야 합니다.");
            return all[0];
        }

        static GameplayCatalog Catalog(ContainerInMap container)
        {
            if (container.CommonCatalog == null || !container.CommonCatalog.RuntimeKeyIsValid()) throw new InvalidOperationException("ContainerInMap의 CommonCatalog 참조가 없습니다.");
            var catalog = AssetDatabase.LoadAssetAtPath<GameplayCatalog>(AssetDatabase.GUIDToAssetPath(container.CommonCatalog.AssetGUID));
            if (catalog == null) throw new InvalidOperationException("ContainerInMap의 CommonCatalog 참조가 없습니다.");
            return catalog;
        }

        static void ValidateRegistration(EntityEditorData data, ContainerInMap container)
        {
            var catalog = Catalog(container);
            if (EditorUtility.IsDirty(catalog)) throw new InvalidOperationException("공통 카탈로그의 미저장 변경을 먼저 저장하세요.");
            if (data.Kind == EntityEditorKind.Monster)
            {
                int id = data.VisualId;
                if ((container.MonsterVisuals != null && id < container.MonsterVisuals.Length && container.MonsterVisuals[id] != null && container.MonsterVisuals[id].RuntimeKeyIsValid()) ||
                    (container.MonsterProfiles != null && id < container.MonsterProfiles.Length && container.MonsterProfiles[id] != null && container.MonsterProfiles[id].RuntimeKeyIsValid()))
                    throw new InvalidOperationException("해당 몬스터 외형 ID는 클라이언트에서 이미 사용 중입니다.");
                if (!catalog.HasSkill(data.AttackSkillId)) throw new InvalidOperationException("공격 스킬이 클라이언트 공통 카탈로그에 없습니다.");
            }
            if (data.Kind == EntityEditorKind.Resource)
            {
                if (catalog.Item(data.ItemId) == null) throw new InvalidOperationException("지급 아이템이 클라이언트 공통 카탈로그에 없습니다.");
                var matches = (catalog.Visuals ?? Array.Empty<ItemVisualEntry>()).Where(v => v != null && v.Item == data.ItemId).ToArray();
                if (matches.Length > 1) throw new InvalidOperationException("공통 카탈로그의 아이템 외형 ID가 중복되어 있습니다.");
                if (matches.Length > 0 && (matches[0].Resource != null || matches[0].ResourceReference != null && matches[0].ResourceReference.RuntimeKeyIsValid()) && !data.ReplaceResourceVisual)
                    throw new InvalidOperationException("같은 지급 아이템의 채집물 외형이 있습니다. 공용 외형 교체에 동의하거나 다른 아이템을 선택하세요.");
            }
        }

        public static string Generate(EntityEditorData data)
        {
            Validate(data);
            string contentPath = MapEditorExport.ContentPath;
            byte[] previousContent = File.ReadAllBytes(contentPath);
            string updatedContent = new EntityContentFile(Encoding.UTF8.GetString(previousContent)).Compose(data);
            string scenePath = AssetDatabase.GetAssetPath(data.RegistrationScene);
            byte[] previousScene = File.ReadAllBytes(scenePath);
            var scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            Scene active = SceneManager.GetActiveScene();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var addedEntries = new List<string>();
            ContainerInMap container = null;
            GameplayCatalog catalog = null;
            string containerJson = null, catalogJson = null;
            bool committed = false;
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject visual = null, placement = null;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                container = Container(scene);
                ValidateRegistration(data, container);
                catalog = Catalog(container);
                containerJson = EditorJsonUtility.ToJson(container);
                catalogJson = EditorJsonUtility.ToJson(catalog);
                EnsureFolder(data.Folder);
                string visualPath = data.Folder + "/" + data.Name + "Visual.prefab";
                var profile = data.Alive ? MakeProfile(data) : null;
                visual = new GameObject(data.Name + "Visual");
                SceneManager.MoveGameObjectToScene(visual, preview);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(data.Model, preview);
                model.transform.SetParent(visual.transform, false);
                model.transform.localPosition = data.Alive ? Vector3.zero : data.VisualOffset;
                model.transform.localRotation = data.Alive ? Quaternion.identity : Quaternion.Euler(data.VisualEuler);
                if (!data.Alive) model.transform.localScale *= data.VisualScale;
                model.SetActive(true);
                foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                { animator.applyRootMotion = false; animator.fireEvents = false; }
                if (profile != null)
                {
                    var animator = model.GetComponentInChildren<Animator>(true) ?? model.AddComponent<Animator>();
                    if (data.Avatar != null) animator.avatar = data.Avatar;
                    animator.runtimeAnimatorController = profile.Controller;
                    animator.applyRootMotion = false; animator.fireEvents = false;
                }
                if (PrefabUtility.SaveAsPrefabAsset(visual, visualPath) == null) throw new IOException("Visual 프리팹 저장에 실패했습니다.");
                Object.DestroyImmediate(visual); visual = null;
                AddAddressable(visualPath, settings, addedEntries);
                if (profile != null) AddAddressable(AssetDatabase.GetAssetPath(profile), settings, addedEntries);
                if (data.Kind != EntityEditorKind.Player)
                {
                    placement = new GameObject(data.Name + "Placement");
                    SceneManager.MoveGameObjectToScene(placement, preview);
                    var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(visualPath), preview);
                    child.transform.SetParent(placement.transform, false);
                    if (profile != null)
                    {
                        child.transform.localPosition = profile.VisualOffset;
                        child.transform.localRotation = Quaternion.Euler(profile.VisualEuler);
                        child.transform.localScale *= profile.VisualScale;
                    }
                    var marker = placement.AddComponent<MapPlacement>();
                    marker.Kind = data.Kind == EntityEditorKind.Monster ? MapPlacementKind.Monster : MapPlacementKind.Resource;
                    marker.DefinitionId = data.DefinitionId;
                    marker.AlignRendererBottom = data.AlignRendererBottom;
                    marker.HeightOffset = data.HeightOffset;
                    marker.OverrideRespawn = data.Kind == EntityEditorKind.Monster && data.OverrideRespawn;
                    marker.RespawnSeconds = data.RespawnSeconds;
                    if (PrefabUtility.SaveAsPrefabAsset(placement, data.Folder + "/" + data.Name + "Placement.prefab") == null) throw new IOException("배치용 프리팹 저장에 실패했습니다.");
                    Object.DestroyImmediate(placement); placement = null;
                }
                Register(data, container, catalog, visualPath, profile);
                SaveReferences(catalog, settings, data.Kind == EntityEditorKind.Resource);
                if (profile != null)
                { AssetDatabase.SaveAssetIfDirty(profile.Controller); AssetDatabase.SaveAssetIfDirty(profile); }
                // 검증 후 외부에서 수정한 원본을 덮어쓰지 않는다.
                if (!File.ReadAllBytes(contentPath).SequenceEqual(previousContent)) throw new IOException("생성 중 서버 콘텐츠 파일이 변경됐습니다. 다시 검증하세요.");
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("등록 씬 저장에 실패했습니다.");
                WriteContent(contentPath, updatedContent);
                committed = true;
                return visualPath;
            }
            catch
            {
                if (!committed)
                {
                    if (container != null && containerJson != null) EditorJsonUtility.FromJsonOverwrite(containerJson, container);
                    if (data.Kind == EntityEditorKind.Resource && catalog != null && catalogJson != null)
                    { EditorJsonUtility.FromJsonOverwrite(catalogJson, catalog); EditorUtility.SetDirty(catalog); }
                    foreach (string guid in addedEntries) settings.RemoveAssetEntry(guid);
                    SaveReferences(catalog, settings, data.Kind == EntityEditorKind.Resource);
                    if (Directory.Exists(data.Folder)) AssetDatabase.DeleteAsset(data.Folder);
                    if (!opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.SaveScene(scene);
                    File.WriteAllBytes(scenePath, previousScene);
                }
                throw;
            }
            finally
            {
                if (visual != null) Object.DestroyImmediate(visual);
                if (placement != null) Object.DestroyImmediate(placement);
                EditorSceneManager.ClosePreviewScene(preview);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }

        static ActionVisualProfile MakeProfile(EntityEditorData data)
        {
            var profile = ScriptableObject.CreateInstance<ActionVisualProfile>();
            profile.name = data.Name + "Profile";
            profile.VisualOffset = data.VisualOffset; profile.VisualEuler = data.VisualEuler; profile.VisualScale = data.VisualScale;
            profile.DeadHeightOffset = data.DeadHeightOffset;
            profile.Damaged = "Hit"; profile.Attacks = new[] { "Skill1" };
            profile.Clips = States.Select(s => new ActionVisualProfile.ClipEntry { State = s, Clip = data.Clip(s) }).ToArray();
            profile.Skills = Enumerable.Range(1, 5).Select(i => data.Clip("Skill" + i)).ToArray();
            profile.SwingEffect = data.SwingEffect; profile.HitEffect = data.HitEffect;
            profile.EffectOffset = data.EffectOffset; profile.SwingCue = data.SwingCue; profile.AttackImpactNormalized = data.AttackImpactNormalized;
            var controller = new AnimatorOverrideController(AssetDatabase.LoadAssetAtPath<AnimatorController>(SharedControllerPath));
            controller.name = data.Name + "Animator";
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; ++i) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, data.Clip(overrides[i].Key.name));
            controller.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(controller, data.Folder + "/" + data.Name + ".overrideController");
            profile.Controller = controller;
            AssetDatabase.CreateAsset(profile, data.Folder + "/" + profile.name + ".asset");
            return profile;
        }

        static void Register(EntityEditorData data, ContainerInMap container, GameplayCatalog catalog, string visualPath, ActionVisualProfile profile)
        {
            var visual = new AssetReferenceGameObject(AssetDatabase.AssetPathToGUID(visualPath));
            var profileRef = profile == null ? null : new AssetReferenceT<ActionVisualProfile>(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)));
            if (data.Kind == EntityEditorKind.Player)
            { container.PlayerVisual = visual; container.PlayerProfile = profileRef; }
            else if (data.Kind == EntityEditorKind.Monster)
            {
                int size = Math.Max(data.VisualId + 1, Math.Max(container.MonsterVisuals?.Length ?? 0, container.MonsterProfiles?.Length ?? 0));
                Array.Resize(ref container.MonsterVisuals, size); Array.Resize(ref container.MonsterProfiles, size);
                container.MonsterVisuals[data.VisualId] = visual; container.MonsterProfiles[data.VisualId] = profileRef;
            }
            else
            {
                var list = (catalog.Visuals ?? Array.Empty<ItemVisualEntry>()).ToList();
                var entry = list.FirstOrDefault(v => v != null && v.Item == data.ItemId);
                if (entry == null) { entry = new ItemVisualEntry { Item = data.ItemId }; list.Add(entry); }
                entry.ResourceReference = visual;
                entry.Resource = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
                catalog.Visuals = list.ToArray();
                EditorUtility.SetDirty(catalog);
            }
            EditorUtility.SetDirty(container);
            EditorSceneManager.MarkSceneDirty(container.gameObject.scene);
        }

        static void AddAddressable(string path, AddressableAssetSettings settings, List<string> added)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (settings.CreateOrMoveEntry(guid, settings.DefaultGroup) == null) throw new InvalidOperationException("Addressables 등록 실패: " + path);
            added.Add(guid);
        }

        static void SaveReferences(GameplayCatalog catalog, AddressableAssetSettings settings, bool saveCatalog)
        {
            if (saveCatalog && catalog != null) AssetDatabase.SaveAssetIfDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
        }

        static void WriteContent(string path, string text)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temporary, text, new UTF8Encoding(false)); File.Replace(temporary, path, null); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("폴더 생성 실패: " + path);
        }

        public static void BuildContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("편집 모드에서 빌드하세요.");
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("미저장 씬을 먼저 저장한 뒤 콘텐츠 빌드를 다시 실행하세요.");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (result == null || !string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result?.Error ?? "콘텐츠 빌드 결과가 없습니다.");
        }
    }
}
