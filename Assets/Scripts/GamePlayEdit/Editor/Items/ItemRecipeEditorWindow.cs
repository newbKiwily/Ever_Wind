using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EverWind.EntityEditing;
using EverWind.Gameplay;
using UnityEditor;
using UnityEngine;

namespace EverWind.ItemEditing
{
    public sealed class ItemRecipeEditorWindow : EditorWindow
    {
        [SerializeField] GameplayCatalog catalog;
        [SerializeField] ItemRecipeEditorData itemData = new ItemRecipeEditorData();
        [SerializeField] ItemRecipeEditorData recipeData = new ItemRecipeEditorData { Mode = ItemRecipeEditorMode.Recipe };
        [SerializeField] int tab, typeIndex;
        [SerializeField] bool choosingType, autoBuild = true;
        [SerializeField] InventoryItem selectedItem;
        [SerializeField] CraftItemRecipe selectedRecipe;
        [SerializeField] string generatedPath;
        Type[] itemTypes;
        Editor itemInspector, recipeInspector;
        Vector2 scroll;
        string feedback;
        MessageType feedbackType;
        bool busy, eventKeys;

        [MenuItem("Tools/EverWind/Item & Recipe Editor")]
        public static void Open() => GetWindow<ItemRecipeEditorWindow>("EverWind 아이템·제작법");

        void OnEnable()
        {
            minSize = new Vector2(480, 600);
            if (catalog == null) catalog = AssetDatabase.LoadAssetAtPath<GameplayCatalog>(ItemRecipeEditorGenerate.CatalogPath);
            if (itemData == null) itemData = new ItemRecipeEditorData();
            if (recipeData == null) recipeData = new ItemRecipeEditorData { Mode = ItemRecipeEditorMode.Recipe };
            itemTypes = ItemRecipeEditorGenerate.ItemTypes();
            typeIndex = Mathf.Clamp(typeIndex, 0, itemTypes.Length - 1);
        }

        void OnDisable() { ReleaseInspectors(); }
        void OnDestroy() { ReleaseInspectors(); Discard(itemData?.Item); Discard(recipeData?.Recipe); }
        void ReleaseInspectors()
        {
            if (itemInspector != null) DestroyImmediate(itemInspector);
            if (recipeInspector != null) DestroyImmediate(recipeInspector);
            itemInspector = null; recipeInspector = null;
        }
        static void Discard(UnityEngine.Object draft) { if (draft != null && !EditorUtility.IsPersistent(draft)) DestroyImmediate(draft); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("아이템 · 제작법 제작", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                catalog = (GameplayCatalog)EditorGUILayout.ObjectField("등록할 공통 카탈로그", catalog, typeof(GameplayCatalog), false);
                itemData.Catalog = catalog; recipeData.Catalog = catalog;
                int nextTab = GUILayout.Toolbar(tab, new[] { "아이템", "제작법" }, GUILayout.Height(28));
                if (nextTab != tab) Queue(() => { tab = nextTab; feedback = null; generatedPath = null; scroll = Vector2.zero; });
                using (var view = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = view.scrollPosition;
                    if (tab == 0) DrawItem(); else DrawRecipe();
                    if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, feedbackType);
                    if (!string.IsNullOrEmpty(generatedPath) && GUILayout.Button("저장한 SO 보기")) EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(generatedPath));
                }
            }
            if (busy) EditorGUILayout.HelpBox("검사·생성 또는 콘텐츠 빌드를 처리하고 있습니다.", MessageType.Info);
        }

        void DrawItem()
        {
            if (GUILayout.Button("+ 아이템 추가", GUILayout.Height(30))) Queue(() => { choosingType = true; feedback = null; generatedPath = null; });
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedItem = (InventoryItem)EditorGUILayout.ObjectField("기존 아이템", selectedItem, typeof(InventoryItem), false);
                using (new EditorGUI.DisabledScope(selectedItem == null || catalog == null))
                    if (GUILayout.Button("불러오기", GUILayout.Width(80))) Queue(LoadItem);
            }
            if (choosingType)
            {
                Section("추가할 InventoryItem SO 타입");
                typeIndex = EditorGUILayout.Popup("타입", typeIndex, itemTypes.Select(t => t == typeof(InventoryItem) ? "InventoryItem (일반 아이템)" : t.Name).ToArray());
                EditorGUILayout.HelpBox("추상 타입은 제외합니다. 선택한 타입의 실제 SO 필드를 아래에서 작성합니다.", MessageType.Info);
                if (GUILayout.Button("선택한 타입으로 작성")) Queue(NewItem);
                return;
            }
            if (itemData.Item == null) { EditorGUILayout.HelpBox("아이템 추가로 타입을 선택하거나 기존 아이템을 불러오세요.", MessageType.Info); return; }
            var data = itemData;
            Section(data.Item.GetType().Name + (data.SourceItem != null ? " · 기존 아이템 수정" : " · 새 아이템"));
            Editor.CreateCachedEditor(data.Item, null, ref itemInspector);
            // 파생 SO의 필드를 중복 정의하지 않고 Unity 직렬화 필드를 그대로 표시한다.
            var serialized = itemInspector.serializedObject;
            serialized.Update();
            var property = serialized.GetIterator();
            bool enter = true;
            while (property.NextVisible(enter))
            {
                enter = false;
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script" || data.SourceItem != null && property.propertyPath == "ItemName"))
                    EditorGUILayout.PropertyField(property, true);
            }
            serialized.ApplyModifiedProperties();
            data.Item.name = data.ItemKey;
            using (new EditorGUI.DisabledScope(data.SourceItem != null)) data.Id = EditorGUILayout.IntField("서버 아이템 ID", data.Id);
            EditorGUILayout.HelpBox("ItemName은 SO 파일명과 서버 key로 사용됩니다. 기존 아이템은 ID·이름·타입을 유지하며 수정합니다.", MessageType.Info);
            eventKeys = EditorGUILayout.Foldout(eventKeys, "퀘스트 이벤트 식별값(선택)");
            if (eventKeys)
            {
                data.GatherEventKey = EditorGUILayout.IntField("채집 이벤트 key", data.GatherEventKey);
                data.CraftEventKey = EditorGUILayout.IntField("제작 이벤트 key", data.CraftEventKey);
                EditorGUILayout.HelpBox("기존 값은 그대로 불러옵니다. 특정 퀘스트 조건에 연결할 때 사용하는 식별값입니다.", MessageType.None);
            }
            Section("채집물 설정");
            bool gather = EditorGUILayout.Toggle("채집 가능한 아이템", data.Gatherable);
            if (gather != data.Gatherable) Queue(() => data.Gatherable = gather);
            if (data.Gatherable) DrawGather(data);
            else if (data.SourceResourceId > 0) EditorGUILayout.HelpBox("체크를 끄면 기존 채집물 정의와 프리팹은 유지하고 아이템 정보만 수정합니다. 맵에서 사용 중인 정의는 자동 삭제하지 않습니다.", MessageType.Info);
            Footer(data);
        }

        void DrawGather(ItemRecipeEditorData data)
        {
            using (new EditorGUI.DisabledScope(data.SourceResourceId > 0)) data.ResourceId = EditorGUILayout.IntField("서버 채집물 정의 ID", data.ResourceId);
            data.ResourceModel = (GameObject)EditorGUILayout.ObjectField("생성 모델 / 외형 프리팹", data.ResourceModel, typeof(GameObject), false);
            data.Capacity = EditorGUILayout.IntField("채집량", data.Capacity);
            data.Radius = EditorGUILayout.FloatField("반경", data.Radius);
            data.GatherTime = EditorGUILayout.FloatField("채집 시간(초)", data.GatherTime);
            data.RefillDelay = EditorGUILayout.FloatField("리필 지연(초)", data.RefillDelay);
            data.VisualOffset = EditorGUILayout.Vector3Field("외형 위치 보정", data.VisualOffset);
            data.VisualEuler = EditorGUILayout.Vector3Field("외형 회전 보정", data.VisualEuler);
            data.VisualScale = EditorGUILayout.FloatField("외형 크기", data.VisualScale);
            data.AlignRendererBottom = EditorGUILayout.Toggle("배치 시 모델 바닥 맞춤", data.AlignRendererBottom);
            data.HeightOffset = EditorGUILayout.FloatField("맵 배치 높이 보정", data.HeightOffset);
            EditorGUILayout.HelpBox("채집물 Visual과 맵 배치용 프리팹을 함께 만듭니다. 같은 지급 아이템을 사용하는 모든 채집물은 공용 외형을 사용합니다.", MessageType.Info);
        }

        void DrawRecipe()
        {
            if (GUILayout.Button("+ 제작법 추가", GUILayout.Height(30))) Queue(NewRecipe);
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedRecipe = (CraftItemRecipe)EditorGUILayout.ObjectField("기존 제작법", selectedRecipe, typeof(CraftItemRecipe), false);
                using (new EditorGUI.DisabledScope(selectedRecipe == null || catalog == null))
                    if (GUILayout.Button("불러오기", GUILayout.Width(80))) Queue(LoadRecipe);
            }
            var data = recipeData;
            if (data.Recipe == null) { EditorGUILayout.HelpBox("제작법을 추가하거나 기존 CraftItemRecipe SO를 불러오세요.", MessageType.Info); return; }
            Section("CraftItemRecipe · 기존 Inspector");
            using (new EditorGUI.DisabledScope(data.SourceRecipe != null))
            {
                data.RecipeName = EditorGUILayout.TextField("SO 파일명", data.RecipeName);
                data.Id = EditorGUILayout.IntField("서버 제작법 ID", data.Id);
            }
            Editor.CreateCachedEditor(data.Recipe, null, ref recipeInspector);
            recipeInspector.OnInspectorGUI();
            EditorGUILayout.HelpBox("ResultItemName은 등록된 결과 아이템의 SO 이름입니다. Ingredients에는 기존처럼 아이템 SO와 수량을 넣으세요.", MessageType.Info);
            Section("서버 제작 설정");
            data.CraftTime = EditorGUILayout.FloatField("제작 시간(초)", data.CraftTime);
            data.OutputCount = EditorGUILayout.IntField("결과 수량", data.OutputCount);
            Footer(data);
        }

        void Footer(ItemRecipeEditorData data)
        {
            Section("저장 및 등록");
            EditorGUILayout.LabelField("SO 저장 경로", data.Mode == ItemRecipeEditorMode.Item ? data.ItemPath : data.RecipePath, EditorStyles.wordWrappedMiniLabel);
            if (data.Mode == ItemRecipeEditorMode.Item && data.Gatherable)
                EditorGUILayout.LabelField("맵 배치 프리팹", data.MapPlacementPath, EditorStyles.wordWrappedMiniLabel);
            autoBuild = EditorGUILayout.Toggle("생성 후 콘텐츠 자동 빌드", autoBuild);
            EditorGUILayout.HelpBox("Generate 전까지는 임시 SO만 편집합니다. 저장하면 서버 GameData.ewdata·공통 카탈로그·필요한 Addressables 참조를 함께 갱신합니다. 서버는 다시 빌드해 콘텐츠를 복사한 뒤 재시작하세요.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("오류 검사")) Queue(() => { ItemRecipeEditorGenerate.Validate(data); Message("입력·참조·출력 경로 검사 통과. 콘텐츠는 저장하지 않았습니다.", MessageType.Info); });
                if (GUILayout.Button("Generate · 저장 및 등록", GUILayout.Height(30))) Queue(() => Generate(data));
            }
            if (!string.IsNullOrEmpty(generatedPath) && GUILayout.Button("콘텐츠 빌드 다시 실행")) Queue(() =>
            {
                EntityEditorGenerate.BuildContent(); Message("콘텐츠 빌드 완료.", MessageType.Info);
            });
        }

        void NewItem()
        {
            var content = EntityContentFile.Read();
            ReleaseInspectors(); Discard(itemData.Item);
            itemData = new ItemRecipeEditorData { Catalog = catalog, Id = NextItemId(content), ResourceId = content.NextId("resource") };
            itemData.Item = (InventoryItem)CreateInstance(itemTypes[typeIndex]);
            itemData.Item.hideFlags = HideFlags.HideAndDontSave; itemData.Item.name = "NewItem"; itemData.Item.ItemName = "NewItem";
            if (itemData.Item is EquipmentItem equipment) equipment.Type = EquipmentItem.EquipmentType.Helmet;
            choosingType = false; feedback = null; generatedPath = null;
        }

        void NewRecipe()
        {
            var content = EntityContentFile.Read();
            ReleaseInspectors(); Discard(recipeData.Recipe);
            int id = Math.Max(content.NextId("recipe"), (catalog?.Recipes ?? Array.Empty<GameplayCatalog.RecipeEntry>()).Select(e => e.Id).DefaultIfEmpty(0).Max() + 1);
            recipeData = new ItemRecipeEditorData { Mode = ItemRecipeEditorMode.Recipe, Catalog = catalog, Id = id };
            recipeData.Recipe = CreateInstance<CraftItemRecipe>(); recipeData.Recipe.hideFlags = HideFlags.HideAndDontSave;
            recipeData.Recipe.name = recipeData.RecipeName; recipeData.Recipe.Ingredients = new List<CraftItemRecipe.Ingredient>();
            feedback = null; generatedPath = null;
        }

        void LoadItem()
        {
            var content = EntityContentFile.Read();
            int id = ItemRecipeEditorGenerate.ItemId(catalog, content, selectedItem);
            var row = content.Row("item", id);
            ReleaseInspectors(); Discard(itemData.Item);
            itemData = new ItemRecipeEditorData { Catalog = catalog, SourceItem = selectedItem, Id = id, Item = Instantiate(selectedItem), ResourceId = content.NextId("resource"), GatherEventKey = I(row[7]), CraftEventKey = I(row[8]) };
            itemData.Item.name = selectedItem.name; itemData.Item.ItemName = selectedItem.name; itemData.Item.hideFlags = HideFlags.HideAndDontSave;
            string[] resource = content.Rows("resource").FirstOrDefault(p => I(p[2]) == id);
            if (resource != null)
            {
                itemData.Gatherable = true; itemData.ResourceId = I(resource[1]); itemData.SourceResourceId = itemData.ResourceId;
                itemData.Capacity = I(resource[3]); itemData.Radius = F(resource[4]); itemData.GatherTime = F(resource[5]); itemData.RefillDelay = F(resource[6]);
                itemData.ResourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ItemRecipeEditorGenerate.ResourceVisualPath(itemData));
            }
            choosingType = false; generatedPath = null;
            Message("원본을 변경하지 않고 불러왔습니다. Generate를 누르면 수정 내용을 반영합니다.", MessageType.Info);
        }

        void LoadRecipe()
        {
            var content = EntityContentFile.Read();
            var matches = (catalog.Recipes ?? Array.Empty<GameplayCatalog.RecipeEntry>()).Where(e => e.Definition == selectedRecipe).ToArray();
            if (matches.Length != 1 || !content.Has("recipe", matches[0].Id)) throw new InvalidOperationException("서버와 카탈로그에 등록된 제작법을 선택하세요.");
            var row = content.Row("recipe", matches[0].Id);
            ReleaseInspectors(); Discard(recipeData.Recipe);
            recipeData = new ItemRecipeEditorData { Mode = ItemRecipeEditorMode.Recipe, Catalog = catalog, SourceRecipe = selectedRecipe, Recipe = Instantiate(selectedRecipe), RecipeName = selectedRecipe.name, Id = matches[0].Id, CraftTime = F(row[2]), OutputCount = I(row[4]) };
            recipeData.Recipe.name = recipeData.RecipeName; recipeData.Recipe.hideFlags = HideFlags.HideAndDontSave;
            generatedPath = null; Message("원본을 변경하지 않고 불러왔습니다. Generate를 누르면 수정 내용을 반영합니다.", MessageType.Info);
        }

        void Generate(ItemRecipeEditorData data)
        {
            generatedPath = ItemRecipeEditorGenerate.Generate(data);
            if (data.Mode == ItemRecipeEditorMode.Item)
            { selectedItem = AssetDatabase.LoadAssetAtPath<InventoryItem>(generatedPath); LoadItem(); }
            else { selectedRecipe = AssetDatabase.LoadAssetAtPath<CraftItemRecipe>(generatedPath); LoadRecipe(); }
            generatedPath = data.Mode == ItemRecipeEditorMode.Item ? AssetDatabase.GetAssetPath(selectedItem) : AssetDatabase.GetAssetPath(selectedRecipe);
            Message("SO·서버 정의·카탈로그 등록 완료: " + generatedPath, MessageType.Info);
            if (!autoBuild) return;
            try { EntityEditorGenerate.BuildContent(); Message("저장·등록·콘텐츠 빌드 완료: " + generatedPath, MessageType.Info); }
            catch (Exception e) { Message("저장·등록은 완료했지만 콘텐츠 빌드에 실패했습니다. 원인을 해결한 뒤 빌드를 다시 실행하세요.\n" + e.Message, MessageType.Error); }
        }

        int NextItemId(EntityContentFile content) => Math.Max(content.NextId("item"), (catalog?.Items ?? Array.Empty<GameplayCatalog.ItemEntry>()).Select(e => e.Id).DefaultIfEmpty(0).Max() + 1);
        void Queue(Action action)
        {
            if (busy) return;
            busy = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                try { action(); }
                catch (Exception e) { Message(e.Message, MessageType.Error); }
                finally { busy = false; Repaint(); }
            };
        }
        void Message(string value, MessageType type) { feedback = value; feedbackType = type; }
        static void Section(string title) { EditorGUILayout.Space(10); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }
        static int I(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
