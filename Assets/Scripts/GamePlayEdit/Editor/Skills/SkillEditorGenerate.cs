using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EverWind.EntityEditing;
using EverWind.Gameplay;
using EverWind.MapEditing;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EverWind.SkillEditing
{
    [Serializable]
    public sealed class SkillEditorData
    {
        public GameplayCatalog Catalog;
        public SkillDefinition Draft, Source;
        public string AssetName = "NewSkill";
        public float[] Multipliers = { 1, 1.1f, 1.2f, 1.3f, 1.4f };
        public float[] Cooldowns = { 1, .95f, .9f, .85f, .8f };
        public float Knockback, DamageRatio = -1, LegacyAreaRadius;
        public string SourceContent;
        public string Path => Source != null ? AssetDatabase.GetAssetPath(Source) : "Assets/Scripts/GamePlayEdit/Skills/" + AssetName + ".asset";
        public string ROPath => "Assets/Scripts/GamePlayEdit/Skills/RO/Skill" + Draft.Id + "RO.prefab";
    }

    public static class SkillEditorGenerate
    {
        public const string CatalogPath = "Assets/Scripts/GamePlayEdit/Common/Catalog.asset";
        static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static void Nonnegative(float v, string name) => EntityContentFile.NonNegative(v, name);
        public static SkillEditorData New(GameplayCatalog catalog, Type type)
        {
            Require(type == typeof(SingleTargetSkill) || type == typeof(RangedSkill) || type == typeof(BuffSkill), "스킬 타입을 선택하세요.");
            var content = EntityContentFile.Read();
            int id = Math.Max(content.NextId("skill"), (catalog?.Skills ?? Array.Empty<GameplayCatalog.SkillEntry>()).Select(e => e.Id).DefaultIfEmpty(0).Max() + 1);
            var draft = (SkillDefinition)ScriptableObject.CreateInstance(type);
            draft.hideFlags = HideFlags.HideAndDontSave; draft.Id = id; draft.AuthoredTiming = true;
            return new SkillEditorData { Catalog = catalog, Draft = draft, SourceContent = File.ReadAllText(MapEditorExport.ContentPath, Encoding.UTF8) };
        }

        public static SkillEditorData Load(GameplayCatalog catalog, SkillDefinition source)
        {
            Require(catalog != null && source != null && EditorUtility.IsPersistent(source), "Project의 등록된 스킬 SO를 지정하세요.");
            var entries = catalog.Skills ?? Array.Empty<GameplayCatalog.SkillEntry>();
            Require(entries.Count(e => e.Id == source.Id && e.Definition == source) == 1 && entries.Count(e => e.Id == source.Id) == 1, "서버와 카탈로그에 같은 ID로 등록된 스킬을 선택하세요.");
            string text = File.ReadAllText(MapEditorExport.ContentPath, Encoding.UTF8);
            var content = new EntityContentFile(text);
            var row = content.Row("skill", source.Id);
            Require((int)source.Kind == int.Parse(row[10], CultureInfo.InvariantCulture), "서버와 SO의 스킬 종류가 다릅니다.");
            var result = new SkillEditorData { Catalog = catalog, Source = source, Draft = Object.Instantiate(source), AssetName = source.name,
                SourceContent = text, Knockback = P(row[7]), DamageRatio = P(row[8]), LegacyAreaRadius = P(row[9]) };
            var s = result.Draft; s.hideFlags = HideFlags.HideAndDontSave; s.name = source.name;
            if (!s.AuthoredTiming)
            {
                s.AttackDuration = P(row[2]); s.AttackTimes = new[] { P(row[3]) }; s.Reach = P(row[4]); s.Damage = P(row[5]); s.Cooldown = P(row[6]);
                s.RequiresTarget = row[11] == "1"; s.AuthoredTiming = true;
                var profile = AssetDatabase.LoadAssetAtPath<ActionVisualProfile>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerProfile.asset");
                if (s.Animation == null && profile != null) s.Animation = profile.ResolveClip("Skill" + s.AnimationSlot);
            }
            foreach (string line in text.Split('\n'))
            {
                var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 5 || p[0] != "skillrank" || p[1] != s.Id.ToString(CultureInfo.InvariantCulture)) continue;
                int rank = int.Parse(p[2], CultureInfo.InvariantCulture) - 1;
                Require(rank >= 0 && rank < 5, "기존 스킬 랭크 범위가 잘못되었습니다.");
                result.Multipliers[rank] = P(p[3]); result.Cooldowns[rank] = P(p[4]);
            }
            return result;
        }
        static float P(string value) => float.Parse(value, CultureInfo.InvariantCulture);
        public static void Validate(SkillEditorData data)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer, "편집 모드에서 컴파일과 빌드가 끝난 뒤 실행하세요.");
            Require(data != null && data.Draft != null && !EditorUtility.IsPersistent(data.Draft), "편집할 스킬을 준비하세요.");
            Require(data.Catalog != null && EditorUtility.IsPersistent(data.Catalog) && !EditorUtility.IsDirty(data.Catalog), "저장된 GameplayCatalog를 지정하세요.");
            var s = data.Draft;
            Require(s is SingleTargetSkill || s is RangedSkill || data.Source != null && s.GetType() == typeof(SkillDefinition), "단일 / RO 스킬을 선택하세요. 버프는 아직 저장·등록하지 않습니다.");
            Require(Enum.IsDefined(typeof(SkillKind), s.Kind) && s.Kind != SkillKind.Buff, "버프 스킬은 작성 틀만 제공하며 실제 저장·등록은 보류합니다.");
            Require(!(s is SingleTargetSkill) || s.Kind == SkillKind.SingleTarget, "단일 대상 SO의 종류를 변경할 수 없습니다.");
            Require(!(s is RangedSkill) || s.Kind == SkillKind.Ranged, "RO SO의 종류를 변경할 수 없습니다.");
            Require(s.AnimationSlot >= 1 && s.AnimationSlot <= 5, "애니메이션 슬롯은 1~5입니다.");
            Require(Regex.IsMatch(data.AssetName ?? "", "^[A-Za-z_][A-Za-z0-9_-]{0,63}$") && !Regex.IsMatch(data.AssetName, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", RegexOptions.IgnoreCase), "SO 이름에는 영문, 숫자, _, -를 사용하세요.");
            Require(s.Id > 0 && s.AuthoredTiming, "양수 ID와 에디터 시간표가 필요합니다.");
            EntityContentFile.Positive(s.AttackDuration, "공격 시간");
            Nonnegative(s.Reach, "사거리"); Nonnegative(s.Damage, "대미지"); Nonnegative(s.Cooldown, "쿨타임");
            Nonnegative(data.Knockback, "넉백"); Nonnegative(data.LegacyAreaRadius, "범위 반경");
            Require(EntityContentFile.Finite(data.DamageRatio) && (data.DamageRatio == -1 || data.DamageRatio >= 0), "피해 배율은 -1 또는 0 이상이어야 합니다.");
            if (s.UsesRO && !(s is RangedSkill)) Require(s.RangeDetector != null && EditorUtility.IsPersistent(s.RangeDetector), "기존 범위 스킬의 탐지 프리팹을 지정하세요.");
            Require(s.Animation != null && EditorUtility.IsPersistent(s.Animation) && s.Animation.isHumanMotion, "유니티짱에 적용할 Humanoid 애니메이션을 넣으세요.");
            Require(s.AttackTimes != null && s.AttackTimes.Length > 0 && s.AttackTimes.Length <= 64, "공격 타이밍은 1~64개가 필요합니다.");
            float previous = -1;
            foreach (float t in s.AttackTimes) { Require(EntityContentFile.Finite(t) && t >= 0 && t <= s.AttackDuration && t > previous, "공격 타이밍은 재생 시간 안에서 오름차순이며 중복이 없어야 합니다."); previous = t; }
            Time(s.CastEffectTime); Time(s.EffectDelay);
            Vector(s.CastEffectOffset); Vector(s.CastEffectRotation);
            Vector(s.SwingEffectOffset); Vector(s.SwingEffectRotation);
            foreach (var effect in new[] { s.CastEffect, s.SwingEffect, (s as RangedSkill)?.ROModel })
                if (effect != null) Require(EditorUtility.IsPersistent(effect) && PrefabUtility.IsPartOfPrefabAsset(effect) && effect.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "효과/RO 모델은 게임플레이 스크립트가 없는 Project 프리팹을 사용하세요.");
            Require(data.Multipliers?.Length == 5 && data.Cooldowns?.Length == 5, "랭크별 배율과 쿨타임 5개가 필요합니다.");
            for (int i = 0; i < 5; ++i) { EntityContentFile.Positive(data.Multipliers[i], "랭크 배율"); Nonnegative(data.Cooldowns[i], "랭크 쿨타임"); }
            var content = EntityContentFile.Read();
            Require(data.SourceContent == null || File.ReadAllText(MapEditorExport.ContentPath, Encoding.UTF8) == data.SourceContent, "작성 중 서버 데이터가 변경되었습니다. 새로 작성하거나 다시 불러오세요.");
            Require(content.Has("skill", s.Id) == (data.Source != null), "서버 ID가 중복되었거나 원본 스킬을 찾을 수 없습니다.");
            var entries = data.Catalog.Skills ?? Array.Empty<GameplayCatalog.SkillEntry>();
            Require(data.Source != null ? entries.Count(e => e.Id == s.Id && e.Definition == data.Source) == 1 && entries.Count(e => e.Id == s.Id) == 1 && data.Source.Id == s.Id : !entries.Any(e => e.Id == s.Id), "카탈로그 ID와 원본 스킬 연결을 확인하세요.");
            if (data.Source != null) Require(!EditorUtility.IsDirty(data.Source) && s.GetType() == data.Source.GetType() && data.AssetName == data.Source.name && s.Kind == data.Source.Kind, "원본을 먼저 저장하세요. 기존 SO 이름·타입·종류는 유지해야 합니다.");
            Require(File.Exists(data.Path) == (data.Source != null) && !Directory.Exists(data.Path), "SO 저장 경로가 기존 파일과 충돌합니다.");
            if (data.Source != null) Require(AssetDatabase.LoadMainAssetAtPath(data.Path) == data.Source && !AssetDatabase.IsSubAsset(data.Source), "독립된 스킬 SO 에셋을 선택하세요.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Require(settings != null && settings.DefaultGroup != null && !EditorUtility.IsDirty(settings.DefaultGroup), "저장된 Addressables 기본 그룹이 필요합니다.");
            if (s is BuffSkill) Require(s.Reach == 0 && s.Damage == 0, "버프 자리표시자의 사거리와 대미지는 0이어야 합니다.");
            if (s is RangedSkill r)
            {
                Require((r.SpawnPoint == ROSpawnPoint.Target || r.SpawnPoint == ROSpawnPoint.Caster) && r.RequiresTarget == r.NeedsTarget, "RO는 몬스터 또는 플레이어 기준으로 생성하며 생성 후 이동을 따라가지 않습니다.");
                Require(!content.Rows("monster").Any(p => int.Parse(p[9]) == s.Id), "몬스터 AI에 연결된 스킬은 RO 스킬로 변경할 수 없습니다.");
                Require(Enum.IsDefined(typeof(ROSpawnPoint), r.SpawnPoint), "RO 생성 위치를 선택하세요.");
                Time(r.SpawnTime); Require(r.SpawnTime <= s.AttackTimes[0], "RO는 첫 타격 이전에 생성되어야 합니다.");
                Vector(r.Offset);
                Vector(r.ROModelOffset); Vector(r.ROModelRotation);
                EntityContentFile.Positive(r.ServerRadius, "서버 검증 반경");
                Require(r.DetectionShapes != null && r.DetectionShapes.Length > 0, "RO 탐지 형태를 하나 이상 추가하세요.");
                foreach (var shape in r.DetectionShapes)
                {
                    Vector(shape.Origin);
                    EntityContentFile.Positive(shape.Radius, "탐지 반경");
                    float extent = shape.Origin.magnitude + shape.Radius;
                    Require(extent <= r.ServerRadius + .001f, "모든 탐지 범위가 서버 검증 반경 안에 있어야 합니다.");
                }
                Require(!File.Exists(data.ROPath) || data.Source is RangedSkill source && source.RangeDetector != null && AssetDatabase.GetAssetPath(source.RangeDetector) == data.ROPath, "RO 출력 경로에 다른 프리팹이 있습니다.");
            }
            void Time(float t) { Require(EntityContentFile.Finite(t) && t >= 0 && t <= s.AttackDuration, "이펙트/생성 시각은 재생 시간 안에 있어야 합니다."); }
        }

        static void Vector(Vector3 v) => Require(EntityContentFile.Finite(v.x) && EntityContentFile.Finite(v.y) && EntityContentFile.Finite(v.z), "벡터에 NaN/Infinity를 사용할 수 없습니다.");

        public static string Compose(SkillEditorData data, EntityContentFile content)
        {
            var s = data.Draft;
            var r = s as RangedSkill;
            var rows = new List<string> { string.Join(" ", "skill", s.Id, F(s.AttackDuration), F(s.AttackTimes[0]), F(s.Reach), F(s.Damage), F(s.Cooldown), F(data.Knockback), F(data.DamageRatio), F(r != null ? r.ServerRadius : data.LegacyAreaRadius), (int)s.Kind, s.NeedsTarget ? 1 : 0) };
            for (int i = 0; i < s.AttackTimes.Length; ++i) rows.Add($"skillhit {s.Id} {i} {F(s.AttackTimes[i])}");
            if (r != null) rows.Add($"skillro {s.Id} {F(r.ServerRadius)} {F(r.SpawnTime)} 0");
            for (int i = 0; i < 5; ++i) rows.Add($"skillrank {s.Id} {i + 1} {F(data.Multipliers[i])} {F(data.Cooldowns[i])}");
            return content.Upsert("skill", s.Id, rows, data.Source != null);
        }

        public static string Generate(SkillEditorData data)
        {
            Validate(data);
            byte[] beforeContent = File.ReadAllBytes(MapEditorExport.ContentPath);
            string text = Compose(data, new EntityContentFile(Encoding.UTF8.GetString(beforeContent)));
            var originals = new Dictionary<string, byte[]>();
            var created = new List<string>();
            var added = new List<string>();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string catalogJson = EditorJsonUtility.ToJson(data.Catalog);
            string sourceJson = data.Source != null ? EditorJsonUtility.ToJson(data.Source) : null;
            SkillDefinition copy = null;
            var scene = EditorSceneManager.NewPreviewScene();
            void Remember(string path)
            {
                if (File.Exists(path)) originals[path] = File.ReadAllBytes(path); else created.Add(path);
                Folder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            }
            void Register(string path)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (settings.FindAssetEntry(guid) != null) return;
                Require(settings.CreateOrMoveEntry(guid, settings.DefaultGroup) != null, "Addressables 등록 실패"); added.Add(guid);
            }
            try
            {
                Remember(AssetDatabase.GetAssetPath(data.Catalog)); Remember(data.Path);
                copy = Object.Instantiate(data.Draft); copy.name = data.AssetName; copy.hideFlags = HideFlags.None;
                if (copy is RangedSkill r)
                {
                    Remember(data.ROPath);
                    var root = BuildRO(r, scene);
                    try
                    {
                        Require(PrefabUtility.SaveAsPrefabAsset(root, data.ROPath) != null, "RO 프리팹 저장 실패");
                    }
                    finally { Object.DestroyImmediate(root); }
                    r.RangeDetector = AssetDatabase.LoadAssetAtPath<GameObject>(data.ROPath).GetComponent<SkillRangeDetector>();
                    Register(data.ROPath);
                }
                SkillDefinition saved;
                if (data.Source == null) { AssetDatabase.CreateAsset(copy, data.Path); saved = copy; copy = null; }
                else { saved = data.Source; EditorUtility.CopySerialized(copy, saved); EditorUtility.SetDirty(saved); AssetDatabase.SaveAssetIfDirty(saved); }
                var entries = (data.Catalog.Skills ?? Array.Empty<GameplayCatalog.SkillEntry>()).Where(e => e.Id != saved.Id).ToList();
                entries.Add(new GameplayCatalog.SkillEntry { Id = saved.Id, Definition = saved, Reach = saved.Reach, Cooldown = saved.Cooldown });
                data.Catalog.Skills = entries.OrderBy(e => e.Id).ToArray();
                Register(data.Path);
                EditorUtility.SetDirty(data.Catalog); AssetDatabase.SaveAssetIfDirty(data.Catalog); AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
                Require(File.ReadAllBytes(MapEditorExport.ContentPath).SequenceEqual(beforeContent), "작업 중 서버 파일이 변경되었습니다. 다시 불러오세요.");
                string temporary = MapEditorExport.ContentPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try { File.WriteAllText(temporary, text, new UTF8Encoding(false)); File.Replace(temporary, MapEditorExport.ContentPath, null); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                return data.Path;
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(catalogJson, data.Catalog);
                if (data.Source != null) EditorJsonUtility.FromJsonOverwrite(sourceJson, data.Source);
                foreach (string guid in added) settings.RemoveAssetEntry(guid);
                AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
                foreach (string path in created.AsEnumerable().Reverse()) if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
                foreach (var file in originals) { File.WriteAllBytes(file.Key, file.Value); AssetDatabase.ImportAsset(file.Key, ImportAssetOptions.ForceUpdate); }
                throw;
            }
            finally { if (copy != null) Object.DestroyImmediate(copy); EditorSceneManager.ClosePreviewScene(scene); }
        }

        public static GameObject BuildRO(RangedSkill skill, Scene scene)
        {
            var root = new GameObject("Skill" + skill.Id + "RO"); SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                if (skill.ROModel != null)
                {
                    Require(skill.ROModel.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "RO 외형에는 스크립트를 포함할 수 없습니다.");
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(skill.ROModel, scene);
                    visual.transform.SetParent(root.transform, false);
                    visual.transform.localPosition = skill.ROModelOffset;
                    visual.transform.localRotation = Quaternion.Euler(skill.ROModelRotation);
                    foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    foreach (var animator in visual.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animator.fireEvents = false; }
                }
                var detector = root.AddComponent<SkillRangeDetector>();
                detector.Shapes = (SkillRangeDetector.Shape[])(skill.DetectionShapes ?? Array.Empty<SkillRangeDetector.Shape>()).Clone();
                return root;
            }
            catch { Object.DestroyImmediate(root); throw; }
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
