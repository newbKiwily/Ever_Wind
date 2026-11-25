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
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EverWind.ItemEditing
{
    public static class ItemRecipeEditorGenerate
    {
        public const string CatalogPath = "Assets/Scripts/GamePlayEdit/Common/Catalog.asset";
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static Type[] ItemTypes() => new[] { typeof(InventoryItem) }.Concat(TypeCache.GetTypesDerivedFrom<InventoryItem>())
            .Where(t => !t.IsAbstract && !t.IsGenericType && t.IsClass).OrderBy(t => t == typeof(InventoryItem) ? "" : t.FullName).ToArray();

        public static void Validate(ItemRecipeEditorData data)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("편집 모드에서 컴파일·빌드가 끝난 후 실행하세요.");
            if (data == null || data.Catalog == null || !EditorUtility.IsPersistent(data.Catalog)) throw new InvalidOperationException("공통 GameplayCatalog 에셋을 지정하세요.");
            if (EditorUtility.IsDirty(data.Catalog)) throw new InvalidOperationException("카탈로그의 미저장 변경을 먼저 저장하세요.");
            if (!Enum.IsDefined(typeof(ItemRecipeEditorMode), data.Mode)) throw new InvalidOperationException("아이템 또는 제작법을 선택하세요.");
            if (data.Id <= 0) throw new InvalidOperationException("정의 ID는 양수여야 합니다.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.DefaultGroup == null || settings.ActivePlayerDataBuilder == null) throw new InvalidOperationException("Addressables 설정·기본 그룹·빌더가 필요합니다.");
            if (EditorUtility.IsDirty(settings.DefaultGroup)) throw new InvalidOperationException("Addressables 기본 그룹의 미저장 변경을 먼저 저장하세요.");
            var content = EntityContentFile.Read();
            if (data.Mode == ItemRecipeEditorMode.Item) ValidateItem(data, content);
            else ValidateRecipe(data, content);
        }

        static void ValidateItem(ItemRecipeEditorData data, EntityContentFile content)
        {
            if (data.Item == null || EditorUtility.IsPersistent(data.Item)) throw new InvalidOperationException("아이템 추가 또는 불러오기로 편집할 SO를 준비하세요.");
            Name(data.ItemKey);
            int kind = ItemKind(data.Item);
            bool existing = data.SourceItem != null;
            var items = data.Catalog.Items ?? Array.Empty<GameplayCatalog.ItemEntry>();
            if (existing)
            {
                if (data.Item.GetType() != data.SourceItem.GetType() || data.ItemKey != data.SourceItem.name) throw new InvalidOperationException("기존 아이템의 타입과 이름/key는 유지하세요.");
                if (items.Count(e => e.Id == data.Id && e.Definition == data.SourceItem) != 1 || items.Count(e => e.Id == data.Id) != 1 || !content.Has("item", data.Id))
                    throw new InvalidOperationException("기존 아이템의 서버 ID와 카탈로그 등록이 일치하지 않습니다.");
                if (Unquote(content.Row("item", data.Id)[2]) != data.ItemKey) throw new InvalidOperationException("서버 key와 기존 아이템 이름이 일치하지 않습니다.");
                if (EditorUtility.IsDirty(data.SourceItem)) throw new InvalidOperationException("원본 아이템의 미저장 변경을 먼저 저장하세요.");
            }
            else if (content.Has("item", data.Id) || items.Any(e => e.Id == data.Id || e.Definition != null && e.Definition.name == data.ItemKey) ||
                content.Rows("item").Any(p => Unquote(p[2]) == data.ItemKey)) throw new InvalidOperationException("아이템 ID 또는 이름/key가 이미 사용 중입니다.");
            Destination(data.ItemPath, existing);
            if (data.Item.ItemImage != null && !EditorUtility.IsPersistent(data.Item.ItemImage)) throw new InvalidOperationException("아이콘은 Project의 Sprite 에셋을 지정하세요.");
            if (data.Item is EquipmentItem equipment && (!Enum.IsDefined(typeof(EquipmentItem.EquipmentType), equipment.Type) || equipment.Power < 0))
                throw new InvalidOperationException("장비 부위와 0 이상의 능력치를 입력하세요.");
            if (data.Item is HPPotion potion) EntityContentFile.NonNegative(potion.HealAmount, "HP 회복량");
            if (kind == 3) Model(((PlaceItem)data.Item).Placement, "설치 외형");
            var visuals = data.Catalog.Visuals ?? Array.Empty<ItemVisualEntry>();
            if (visuals.Count(v => v != null && v.Item == data.Id) > 1) throw new InvalidOperationException("카탈로그의 아이템 외형 ID가 중복되어 있습니다.");
            if (data.Gatherable)
            {
                if (data.ResourceId <= 0 || data.Capacity <= 0) throw new InvalidOperationException("채집물 정의 ID와 채집량은 양수여야 합니다.");
                bool updateResource = existing && data.SourceResourceId > 0;
                if (updateResource)
                {
                    if (data.SourceResourceId != data.ResourceId || !content.Has("resource", data.ResourceId) || I(content.Row("resource", data.ResourceId)[2]) != data.Id)
                        throw new InvalidOperationException("기존 채집물 ID와 지급 아이템 연결을 유지하세요.");
                }
                else if (content.Has("resource", data.ResourceId)) throw new InvalidOperationException("채집물 정의 ID가 이미 사용 중입니다.");
                EntityContentFile.Positive(data.Radius, "채집물 반경");
                EntityContentFile.NonNegative(data.GatherTime, "채집 시간");
                EntityContentFile.NonNegative(data.RefillDelay, "리필 지연");
                EntityContentFile.Positive(data.VisualScale, "외형 크기");
                Vector(data.VisualOffset); Vector(data.VisualEuler);
                if (!EntityContentFile.Finite(data.HeightOffset)) throw new InvalidOperationException("배치 높이는 유한한 값이어야 합니다.");
                Model(data.ResourceModel, "채집물 외형");
                Destination(ResourceVisualPath(data), existing && File.Exists(ResourceVisualPath(data)));
                Destination(data.MapPlacementPath, existing && File.Exists(data.MapPlacementPath));
            }
            if (kind == 3) Destination(PlacementVisualPath(data), existing && File.Exists(PlacementVisualPath(data)));
            if (data.Gatherable && kind == 3 && string.Equals(ResourceVisualPath(data), PlacementVisualPath(data), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("채집물과 설치물의 출력 프리팹 경로를 각각 분리하세요.");
        }

        static void ValidateRecipe(ItemRecipeEditorData data, EntityContentFile content)
        {
            if (data.Recipe == null || EditorUtility.IsPersistent(data.Recipe)) throw new InvalidOperationException("제작법 추가 또는 불러오기로 편집할 SO를 준비하세요.");
            Name(data.RecipeName);
            bool existing = data.SourceRecipe != null;
            var recipes = data.Catalog.Recipes ?? Array.Empty<GameplayCatalog.RecipeEntry>();
            if (existing)
            {
                if (data.RecipeName != data.SourceRecipe.name || recipes.Count(e => e.Id == data.Id && e.Definition == data.SourceRecipe) != 1 || recipes.Count(e => e.Id == data.Id) != 1 || !content.Has("recipe", data.Id))
                    throw new InvalidOperationException("기존 제작법의 이름·서버 ID·카탈로그 등록을 유지하세요.");
                if (EditorUtility.IsDirty(data.SourceRecipe)) throw new InvalidOperationException("원본 제작법의 미저장 변경을 먼저 저장하세요.");
            }
            else if (content.Has("recipe", data.Id) || recipes.Any(e => e.Id == data.Id || e.Definition != null && e.Definition.name == data.RecipeName))
                throw new InvalidOperationException("제작법 ID 또는 이름이 이미 사용 중입니다.");
            Destination(data.RecipePath, existing);
            EntityContentFile.NonNegative(data.CraftTime, "제작 시간");
            if (data.OutputCount <= 0) throw new InvalidOperationException("제작 결과 수량은 양수여야 합니다.");
            ResolveOutput(data, content);
            if (data.Recipe.Ingredients == null || data.Recipe.Ingredients.Count == 0) throw new InvalidOperationException("제작 재료를 한 개 이상 지정하세요.");
            var ids = new HashSet<int>();
            foreach (var ingredient in data.Recipe.Ingredients)
            {
                int id = ItemId(data.Catalog, content, ingredient.IngredientItem);
                if (ingredient.Amount <= 0) throw new InvalidOperationException("재료 수량은 양수여야 합니다.");
                if (!ids.Add(id)) throw new InvalidOperationException("같은 재료는 한 행으로 합쳐서 입력하세요.");
            }
        }

        public static int ItemKind(InventoryItem item)
        {
            if (item is EquipmentItem) return 1;
            if (item is HPPotion) return 2;
            if (item is PlaceItem) return 3;
            if (item is ConsumeItem) throw new InvalidOperationException("이 소비 아이템 타입은 아직 서버 사용 효과에 대응하지 않습니다.");
            return 0;
        }

        public static int ItemId(GameplayCatalog catalog, EntityContentFile content, InventoryItem item)
        {
            if (item == null || !EditorUtility.IsPersistent(item)) throw new InvalidOperationException("등록된 아이템 SO 에셋을 지정하세요.");
            var entries = (catalog.Items ?? Array.Empty<GameplayCatalog.ItemEntry>()).Where(e => e.Definition == item).ToArray();
            if (entries.Length != 1 || entries[0].Id <= 0 || !content.Has("item", entries[0].Id) || Unquote(content.Row("item", entries[0].Id)[2]) != item.name)
                throw new InvalidOperationException("아이템이 서버·카탈로그에 같은 ID/key로 등록되어 있지 않습니다: " + item.name);
            return entries[0].Id;
        }

        static int ResolveOutput(ItemRecipeEditorData data, EntityContentFile content)
        {
            var matches = (data.Catalog.Items ?? Array.Empty<GameplayCatalog.ItemEntry>()).Where(e => e.Definition != null && e.Definition.name == data.Recipe.ResultItemName).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("ResultItemName에 등록된 결과 아이템의 에셋 이름을 정확히 입력하세요.");
            return ItemId(data.Catalog, content, matches[0].Definition);
        }

        public static string Compose(ItemRecipeEditorData data, EntityContentFile content)
        {
            if (data.Mode == ItemRecipeEditorMode.Recipe)
            {
                ValidateRecipe(data, content);
                var block = new List<string> { string.Join(" ", "recipe", data.Id, F(data.CraftTime), ResolveOutput(data, content), data.OutputCount) };
                for (int i = 0; i < data.Recipe.Ingredients.Count; ++i)
                {
                    var ingredient = data.Recipe.Ingredients[i];
                    block.Add(string.Join(" ", "ingredient", data.Id, i, ItemId(data.Catalog, content, ingredient.IngredientItem), ingredient.Amount));
                }
                return content.Upsert("recipe", data.Id, block, data.SourceRecipe != null);
            }
            ValidateItem(data, content);
            int kind = ItemKind(data.Item);
            int slot = data.Item is EquipmentItem equipment ? (int)equipment.Type : 0;
            int power = data.Item is EquipmentItem gear ? gear.Power : 0;
            float heal = data.Item is HPPotion potion ? potion.HealAmount : 0;
            string row = string.Join(" ", "item", data.Id, "\"" + data.ItemKey + "\"", kind, slot, power, F(heal), data.GatherEventKey, data.CraftEventKey);
            string result = content.Upsert("item", data.Id, new[] { row }, data.SourceItem != null);
            if (data.Gatherable)
            {
                row = string.Join(" ", "resource", data.ResourceId, data.Id, data.Capacity, F(data.Radius), F(data.GatherTime), F(data.RefillDelay));
                result = new EntityContentFile(result).Upsert("resource", data.ResourceId, new[] { row }, data.SourceItem != null && data.SourceResourceId > 0);
            }
            return result;
        }

        static ItemVisualEntry Visual(ItemRecipeEditorData data) => (data.Catalog.Visuals ?? Array.Empty<ItemVisualEntry>()).FirstOrDefault(v => v != null && v.Item == data.Id);
        public static string ResourceVisualPath(ItemRecipeEditorData data) => ExistingVisual(data, false) ?? data.VisualFolder + "/" + data.ItemKey + "ResourceVisual.prefab";
        public static string PlacementVisualPath(ItemRecipeEditorData data) => ExistingVisual(data, true) ?? data.VisualFolder + "/" + data.ItemKey + "PlacementVisual.prefab";

        static string ExistingVisual(ItemRecipeEditorData data, bool placement)
        {
            if (data.SourceItem == null) return null;
            var visual = Visual(data);
            var reference = placement ? visual?.PlacementReference : visual?.ResourceReference;
            string path = reference != null && reference.RuntimeKeyIsValid() ? AssetDatabase.GUIDToAssetPath(reference.AssetGUID) : null;
            if (string.IsNullOrEmpty(path)) path = AssetDatabase.GetAssetPath(placement ? visual?.Placement : visual?.Resource);
            return !string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        static void Model(GameObject model, string label)
        {
            if (model == null || !EditorUtility.IsPersistent(model) || !AssetDatabase.IsMainAsset(model) || !PrefabUtility.IsPartOfPrefabAsset(model))
                throw new InvalidOperationException(label + "에 Project의 모델/프리팹 루트 에셋을 지정하세요.");
            if (model.GetComponentsInChildren<Renderer>(true).Length == 0 || model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException(label + "은 Renderer가 있고 게임플레이/누락 스크립트가 없는 외형 전용 에셋이어야 합니다.");
        }

        static void Name(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-') ||
                Regex.IsMatch(name, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", RegexOptions.IgnoreCase))
                throw new InvalidOperationException("이름/key는 64자 이내의 글자·숫자·밑줄·하이픈을 사용하고 운영체제 예약 이름은 피하세요.");
        }

        static void Destination(string path, bool overwrite)
        {
            bool exists = File.Exists(path);
            if (exists != overwrite || Directory.Exists(path) || !overwrite && File.Exists(path + ".meta"))
                throw new InvalidOperationException("출력 경로가 기존 파일과 충돌하거나 수정할 파일이 없습니다: " + path);
            if (overwrite && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new InvalidOperationException("읽기 전용 출력 파일입니다: " + path);
        }

        static void Vector(Vector3 v)
        {
            if (!EntityContentFile.Finite(v.x) || !EntityContentFile.Finite(v.y) || !EntityContentFile.Finite(v.z)) throw new InvalidOperationException("외형 보정에 NaN/Infinity를 사용할 수 없습니다.");
        }

        public static string Generate(ItemRecipeEditorData data)
        {
            Validate(data);
            string contentPath = MapEditorExport.ContentPath;
            byte[] previousContent = File.ReadAllBytes(contentPath);
            string updatedContent = Compose(data, new EntityContentFile(Encoding.UTF8.GetString(previousContent)));
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var originals = new Dictionary<string, byte[]>();
            var created = new List<string>();
            var addedEntries = new List<string>();
            string catalogJson = EditorJsonUtility.ToJson(data.Catalog);
            Object source = data.Mode == ItemRecipeEditorMode.Item ? (Object)data.SourceItem : data.SourceRecipe;
            string sourceJson = source != null ? EditorJsonUtility.ToJson(source) : null;
            void Remember(string path)
            {
                if (File.Exists(path)) originals[path] = File.ReadAllBytes(path);
                else created.Add(path);
                EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            }
            void Addressable(string path)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (settings.FindAssetEntry(guid) != null) return;
                if (settings.CreateOrMoveEntry(guid, settings.DefaultGroup) == null) throw new InvalidOperationException("Addressables 등록 실패: " + path);
                addedEntries.Add(guid);
            }
            string outputPath = data.Mode == ItemRecipeEditorMode.Item ? data.ItemPath : data.RecipePath;
            var preview = EditorSceneManager.NewPreviewScene();
            Object copy = null;
            try
            {
                Remember(AssetDatabase.GetAssetPath(data.Catalog));
                Remember(outputPath);
                if (data.Mode == ItemRecipeEditorMode.Item)
                {
                    var item = Object.Instantiate(data.Item); copy = item;
                    item.name = data.ItemKey; item.ItemName = data.ItemKey; item.hideFlags = HideFlags.None;
                    ItemVisualEntry visual = null;
                    if (data.Gatherable || item is PlaceItem)
                    {
                        var visuals = (data.Catalog.Visuals ?? Array.Empty<ItemVisualEntry>()).ToList();
                        visual = visuals.FirstOrDefault(v => v != null && v.Item == data.Id);
                        if (visual == null) { visual = new ItemVisualEntry { Item = data.Id }; visuals.Add(visual); }
                        data.Catalog.Visuals = visuals.ToArray();
                    }
                    if (data.Gatherable)
                    {
                        string path = ResourceVisualPath(data); Remember(path);
                        SaveVisual(data.ResourceModel, path, data.VisualOffset, data.VisualEuler, data.VisualScale, preview);
                        Addressable(path);
                        visual.Resource = null; visual.ResourceReference = new AssetReferenceGameObject(AssetDatabase.AssetPathToGUID(path));
                        Remember(data.MapPlacementPath);
                        SaveMapPlacement(data, path, preview);
                    }
                    if (item is PlaceItem place)
                    {
                        string path = PlacementVisualPath(data); Remember(path);
                        SaveVisual(place.Placement, path, Vector3.zero, Vector3.zero, 1, preview);
                        Addressable(path);
                        place.Placement = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        visual.Placement = null; visual.PlacementReference = new AssetReferenceGameObject(AssetDatabase.AssetPathToGUID(path));
                    }
                    if (data.SourceItem == null)
                    {
                        AssetDatabase.CreateAsset(item, outputPath);
                        if (!EditorUtility.IsPersistent(item)) throw new IOException("아이템 SO 저장에 실패했습니다.");
                        copy = null;
                        data.Catalog.Items = (data.Catalog.Items ?? Array.Empty<GameplayCatalog.ItemEntry>()).Concat(new[] { new GameplayCatalog.ItemEntry { Id = data.Id, Definition = item } }).ToArray();
                    }
                    else
                    {
                        EditorUtility.CopySerialized(item, data.SourceItem);
                        data.SourceItem.hideFlags = HideFlags.None;
                        EditorUtility.SetDirty(data.SourceItem); AssetDatabase.SaveAssetIfDirty(data.SourceItem);
                    }
                }
                else
                {
                    var recipe = Object.Instantiate(data.Recipe); copy = recipe;
                    recipe.name = data.RecipeName; recipe.hideFlags = HideFlags.None;
                    if (data.SourceRecipe == null)
                    {
                        AssetDatabase.CreateAsset(recipe, outputPath);
                        if (!EditorUtility.IsPersistent(recipe)) throw new IOException("제작법 SO 저장에 실패했습니다.");
                        copy = null;
                        data.Catalog.Recipes = (data.Catalog.Recipes ?? Array.Empty<GameplayCatalog.RecipeEntry>()).Concat(new[] { new GameplayCatalog.RecipeEntry { Id = data.Id, Definition = recipe } }).ToArray();
                    }
                    else
                    {
                        EditorUtility.CopySerialized(recipe, data.SourceRecipe);
                        data.SourceRecipe.hideFlags = HideFlags.None;
                        EditorUtility.SetDirty(data.SourceRecipe); AssetDatabase.SaveAssetIfDirty(data.SourceRecipe);
                    }
                }
                Addressable(outputPath);
                EditorUtility.SetDirty(data.Catalog); AssetDatabase.SaveAssetIfDirty(data.Catalog);
                AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
                if (!File.ReadAllBytes(contentPath).SequenceEqual(previousContent)) throw new IOException("처리 중 서버 콘텐츠 파일이 바뀌었습니다. 다시 불러오세요.");
                string temporary = contentPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try { File.WriteAllText(temporary, updatedContent, new UTF8Encoding(false)); File.Replace(temporary, contentPath, null); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                return outputPath;
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(catalogJson, data.Catalog);
                if (source != null) EditorJsonUtility.FromJsonOverwrite(sourceJson, source);
                foreach (string guid in addedEntries) settings.RemoveAssetEntry(guid);
                AssetDatabase.SaveAssetIfDirty(settings.DefaultGroup);
                foreach (string path in created.AsEnumerable().Reverse()) if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
                foreach (var entry in originals) { File.WriteAllBytes(entry.Key, entry.Value); AssetDatabase.ImportAsset(entry.Key, ImportAssetOptions.ForceUpdate); }
                throw;
            }
            finally
            {
                if (copy != null) Object.DestroyImmediate(copy);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static void SaveVisual(GameObject model, string path, Vector3 offset, Vector3 euler, float scale, Scene scene)
        {
            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                // 수정 대상 프리팹 자신을 중첩 참조하지 않도록 외형 복사본의 연결만 해제한다.
                PrefabUtility.UnpackPrefabInstance(child, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = offset; child.transform.localRotation = Quaternion.Euler(euler); child.transform.localScale *= scale;
                child.SetActive(true);
                foreach (var animator in child.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animator.fireEvents = false; }
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new IOException("외형 프리팹 저장 실패: " + path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void SaveMapPlacement(ItemRecipeEditorData data, string visualPath, Scene scene)
        {
            var root = new GameObject(data.ItemKey + "MapPlacement");
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(visualPath), scene);
                child.transform.SetParent(root.transform, false);
                var marker = root.AddComponent<MapPlacement>();
                marker.Kind = MapPlacementKind.Resource; marker.DefinitionId = data.ResourceId;
                marker.AlignRendererBottom = data.AlignRendererBottom; marker.HeightOffset = data.HeightOffset;
                if (PrefabUtility.SaveAsPrefabAsset(root, data.MapPlacementPath) == null) throw new IOException("맵 배치용 프리팹 저장 실패");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path)))) throw new IOException("폴더 생성 실패: " + path);
        }

        public static string Unquote(string value) => value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"' ? value.Substring(1, value.Length - 2) : value;
        static int I(string value) => int.Parse(value, Culture);
        static string F(float value) => value.ToString("R", Culture);
    }
}
