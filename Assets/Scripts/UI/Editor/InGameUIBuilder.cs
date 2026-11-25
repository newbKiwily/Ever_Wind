#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EverWind.Gameplay;

public static class InGameUIBuilder
{
    const string Folder = "Assets/Scripts/UI/InGame/Prefabs";
    const string Existing = "Assets/Scripts/GamePlayEdit/UI/";
    const string Art = "Assets/UIImage/";
    static TMP_FontAsset font;
    static Color Ink = new Color(.06f, .09f, .13f, .97f);
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
    static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position, Vector2? anchor = null)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        return go;
    }
    static Image Image(GameObject go, Sprite sprite, Color? color = null)
    {
        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>(); image.sprite = sprite; image.color = color ?? Color.white; image.raycastTarget = false;
        return image;
    }
    static TMP_Text Text(string name, Transform parent, string value, Vector2 size, Vector2 pos, float scale = 22, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var text = Rect(name, parent, size, pos).AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = scale;
        text.color = new Color(.96f, .95f, .9f); text.alignment = alignment; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal; return text;
    }
    static UIControlButton Button(string name, Transform parent, string title, Vector2 size, Vector2 pos, string id)
    {
        var go = Rect(name, parent, size, pos); var image = Image(go, Sprite("버튼.png"), new Color(.5f, .65f, .65f)); image.raycastTarget = true;
        var button = go.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var control = go.AddComponent<UIControlButton>(); control.ControlId = id;
        Text("Label", go.transform, title, size - new Vector2(8, 4), Vector2.zero, 21); return control;
    }
    static void Layout(Transform parent, Vector2 size, int columns)
    {
        var layout = parent.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize = size; layout.spacing = new Vector2(12, 12); layout.padding = new RectOffset(12, 12, 12, 12);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = columns;
        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    static Transform Scroll(Transform parent, string name, Vector2 size, Vector2 pos, Vector2 cell, int columns)
    {
        var root = Rect(name, parent, size, pos); var scroll = root.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 30;
        var viewport = Rect("Viewport", root.transform, size, Vector2.zero); viewport.AddComponent<RectMask2D>(); Image(viewport, null, Color.clear).raycastTarget = true;
        var content = Rect("Content", viewport.transform, new Vector2(size.x, 0), Vector2.zero, new Vector2(.5f, 1));
        var rect = (RectTransform)content.transform; rect.pivot = new Vector2(.5f, 1); Layout(content.transform, cell, columns);
        scroll.viewport = (RectTransform)viewport.transform; scroll.content = rect;
        return content.transform;
    }
    static Slot Template<T>(string name, string original, UISlotKind kind, Vector2 size) where T : Slot
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Existing + original);
        var go = source != null ? UnityEngine.Object.Instantiate(source) : new GameObject(name, typeof(RectTransform));
        go.name = name; go.hideFlags = HideFlags.HideAndDontSave;
        foreach (var old in go.GetComponents<MonoBehaviour>()) if (!(old is UnityEngine.UI.Graphic) && !(old is UnityEngine.UI.Selectable)) UnityEngine.Object.DestroyImmediate(old);
        foreach (Transform child in go.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = size;
        go.layer = 5; var image = Image(go, Sprite(kind == UISlotKind.Recipe ? "제작슬롯바탕패널.png" : "slot.png"), new Color(.48f, .57f, .58f)); image.raycastTarget = true;
        var button = go.GetComponent<UnityEngine.UI.Button>() ?? go.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var slot = go.AddComponent<T>(); slot.Kind = kind;
        if (kind != UISlotKind.Quest)
        {
            slot.ItemImage = Image(Rect("ItemSlot", go.transform, new Vector2(50, 50), new Vector2(0, 10)), null); slot.ItemImage.enabled = false;
            slot.NameText = Text("ItemName", go.transform, "", new Vector2(size.x - 12, 24), new Vector2(0, -size.y / 2 + 17), 18);
            slot.CountText = Text("Count", go.transform, "", new Vector2(size.x - 10, 24), new Vector2(0, size.y / 2 - 16), 18);
        }
        else
        {
            slot.NameText = Text("QuestName", go.transform, "", new Vector2(size.x - 26, 30), new Vector2(0, size.y / 2 - 22), 22, TextAlignmentOptions.Left);
        }
        GameObject prefab;
        try { go.hideFlags = HideFlags.None; prefab = PrefabUtility.SaveAsPrefabAsset(go, Folder + "/" + name + ".prefab"); }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        return prefab.GetComponent<T>();
    }
    static GameObject Panel(Transform canvas, string name, string title, Vector2 size, Sprite art)
    {
        var panel = Rect(name, canvas, size, Vector2.zero); Image(panel, art, Ink).raycastTarget = true;
        Text("Title", panel.transform, title, new Vector2(size.x - 100, 42), new Vector2(0, size.y / 2 - 35), 30);
        var close = Button("Close", panel.transform, "×", new Vector2(44, 38), new Vector2(size.x / 2 - 34, size.y / 2 - 30), name + ":close"); close.OpensPanel = true; close.Panel = InGamePanel.None;
        return panel;
    }
    [MenuItem("Tools/EverWind/Rebuild In-game UI")]
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("플레이 모드를 종료한 뒤 실행하세요.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset");
        if (font == null) throw new InvalidOperationException("NotoSansKR SDF가 없습니다.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("InGame 씬의 기존 미저장 변경을 먼저 저장하세요.");
        var oldRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "InGame UI");
        if (oldRoot != null) throw new InvalidOperationException("인게임 UI가 이미 있습니다. 기존 연결을 확인하세요.");
        var item = Template<InventorySlot>("ItemSlot", "InventoryUI/ItemSlot.prefab", UISlotKind.Inventory, new Vector2(100, 100));
        var equipped = Template<EquipmentSlot>("EquipmentSlot", "InventoryUI/ItemSlot.prefab", UISlotKind.Equipment, new Vector2(100, 100));
        var recipe = Template<CraftSlot>("CraftItemSlot", "CraftUI/CraftItemSlot.prefab", UISlotKind.Recipe, new Vector2(220, 95));
        var ingredient = Template<IngredientSlot>("IngredientSlot", "CraftUI/IngredientSlot.prefab", UISlotKind.Ingredient, new Vector2(180, 100));
        var quest = Template<QuestContentUI>("QuestContent", "QuestUI/QuestContent.prefab", UISlotKind.Quest, new Vector2(680, 125));
        var root = new GameObject("InGame UI"); SceneManager.MoveGameObjectToScene(root, scene);
        var uiCamera = new GameObject("UI Camera").AddComponent<Camera>(); uiCamera.transform.SetParent(root.transform, false); uiCamera.transform.position = new Vector3(0, 0, -1000);
        uiCamera.orthographic = true; uiCamera.orthographicSize = 540; uiCamera.cullingMask = 1 << 5; uiCamera.clearFlags = CameraClearFlags.Depth; uiCamera.nearClipPlane = .1f; uiCamera.farClipPlane = 100; uiCamera.depth = 20;
        var main = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>()).First(camera => camera.CompareTag("MainCamera"));
        main.cullingMask &= ~(1 << 5);
        var canvasObject = Rect("Canvas", root.transform, new Vector2(1920, 1080), Vector2.zero);
        var canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 10;
        canvasObject.AddComponent<GraphicRaycaster>(); var scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var controller = canvasObject.AddComponent<InGameUIController>(); controller.Canvas = canvas;
        var presenter = canvasObject.AddComponent<UIEffectPresenter>(); presenter.UICamera = uiCamera;
        var hud = Rect("HUD", canvas.transform, new Vector2(1920, 1080), Vector2.zero); ((RectTransform)hud.transform).anchorMin = Vector2.zero; ((RectTransform)hud.transform).anchorMax = Vector2.one; ((RectTransform)hud.transform).sizeDelta = Vector2.zero;
        var display = hud.AddComponent<DisplayUIManager>();
        var vitals = Rect("Vitals", hud.transform, new Vector2(390, 130), new Vector2(225, -95), new Vector2(0, 1)); Image(vitals, Sprite("IngameUI/IngameImage/HP_UI.png"), Ink);
        Image(Rect("Profile", vitals.transform, new Vector2(85, 85), new Vector2(-140, 0)), Sprite("IngameUI/NormalProfile.png"));
        Image(Rect("HealthBackground", vitals.transform, new Vector2(250, 24), new Vector2(45, 7)), Sprite("IngameUI/IngameImage/HP_Emty.png"));
        display.HpBar = Image(Rect("HealthFill", vitals.transform, new Vector2(250, 24), new Vector2(45, 7)), Sprite("IngameUI/IngameImage/HP.png")); display.HpBar.type = UnityEngine.UI.Image.Type.Filled; display.HpBar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        display.Health = Text("Health", vitals.transform, "접속 준비 중", new Vector2(250, 26), new Vector2(45, 7), 19);
        display.Growth = Text("Growth", vitals.transform, "Lv. 0", new Vector2(260, 26), new Vector2(45, -32), 17);
        display.Target = Text("SelectedTarget", hud.transform, "", new Vector2(600, 38), new Vector2(0, -50), 24); ((RectTransform)display.Target.transform).anchorMin = ((RectTransform)display.Target.transform).anchorMax = new Vector2(.5f, 1);
        var skillBar = Rect("Skills", hud.transform, new Vector2(610, 100), new Vector2(0, 82), new Vector2(.5f, 0));
        string[] skillIcons = { "icons_skills_fighter", "icons_skills_powerstrike", "icons_skills_runningstrike", "icons_skills_punisher", "icons_skills_revive" };
        for (int i = 0; i < 5; ++i)
        {
            var go = Rect("Skill " + (i + 1), skillBar.transform, new Vector2(105, 90), new Vector2((i - 2) * 117, 0));
            var image = Image(go, Sprite("IngameUI/IngameImage/Skill.png")); image.raycastTarget = true; go.AddComponent<UnityEngine.UI.Button>().targetGraphic = image;
            Image(Rect("Icon", go.transform, new Vector2(45, 45), new Vector2(0, 12)), Sprite("IngameUI/Icons/skills/" + skillIcons[i] + ".png"));
            var button = go.AddComponent<SkillButton>(); button.Value = i + 1; button.ControlId = "skill:" + (i + 1);
            button.Label = Text("Label", go.transform, (i + 1).ToString(), new Vector2(100, 30), new Vector2(0, -26), 17);
            button.CooldownImage = Image(Rect("Cooldown", go.transform, new Vector2(98, 84), Vector2.zero), null, new Color(0, 0, 0, .65f)); button.CooldownImage.type = UnityEngine.UI.Image.Type.Filled; button.CooldownImage.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        }
        var menu = Rect("Menu", hud.transform, new Vector2(500, 50), new Vector2(-270, 45), new Vector2(1, 0));
        string[] menuNames = { "가방 [I]", "제작 [C]", "퀘스트 [Q]", "로그아웃" }; string[] ids = { "inventory", "craft", "quest", "logout" };
        for (int i = 0; i < 4; ++i) { var b = Button(ids[i], menu.transform, menuNames[i], new Vector2(112, 42), new Vector2((i - 1.5f) * 122, 0), ids[i]); b.OpensPanel = i < 3; b.Panel = (InGamePanel)(i + 1); b.Command = UICommandKind.Logout; }
        var inventory = Panel(canvas.transform, "Inventory", "소지품 / 장비", new Vector2(920, 640), Sprite("inventorypanel.png"));
        var inventoryView = inventory.AddComponent<Inventory>(); inventoryView.SlotPrefab = item; inventoryView.Content = Scroll(inventory.transform, "Items", new Vector2(590, 500), new Vector2(-140, -20), new Vector2(100, 100), 5);
        var equipmentView = Rect("Equipment", inventory.transform, new Vector2(250, 470), new Vector2(315, -20)).AddComponent<EquipmentUI>(); Image(equipmentView.gameObject, Sprite("equipUI.png"), new Color(.3f, .35f, .4f, .65f));
        equipmentView.Content = Scroll(equipmentView.transform, "Slots", new Vector2(244, 440), Vector2.zero, new Vector2(100, 100), 2); equipmentView.SlotPrefab = equipped;
        var craft = Panel(canvas.transform, "Craft", "제작", new Vector2(940, 650), Sprite("제작-배경.png")); var craftView = craft.AddComponent<CraftUI>(); craftView.SlotPrefab = recipe;
        craftView.Content = Scroll(craft.transform, "Recipes", new Vector2(280, 500), new Vector2(-305, -20), new Vector2(235, 100), 1);
        craftView.IngredientContent = Scroll(craft.transform, "Ingredients", new Vector2(560, 360), new Vector2(135, -15), new Vector2(245, 100), 2); craftView.IngredientPrefab = ingredient;
        craftView.SelectedName = Text("SelectedRecipe", craft.transform, "제작법을 선택하세요", new Vector2(550, 45), new Vector2(135, 205), 25);
        var craftButton = Button("Craft", craft.transform, "제작하기", new Vector2(230, 50), new Vector2(135, -255), "craft:execute"); craftButton.Command = UICommandKind.Craft;
        var quests = Panel(canvas.transform, "Quests", "퀘스트", new Vector2(780, 660), Sprite("바탕패널.png")); var questView = quests.AddComponent<QuestUI>(); questView.SlotPrefab = quest; questView.Content = Scroll(quests.transform, "QuestList", new Vector2(740, 530), new Vector2(0, -20), new Vector2(680, 125), 1);
        var dead = Panel(canvas.transform, "Dead", "쓰러졌습니다", new Vector2(540, 300), Sprite("바탕패널.png")); var deadView = dead.AddComponent<DeadUI>(); deadView.Remaining = Text("Remaining", dead.transform, "", new Vector2(480, 60), Vector2.zero, 24);
        var revive = Button("Revive", dead.transform, "부활", new Vector2(190, 50), new Vector2(0, -90), "revive"); revive.Command = UICommandKind.Revive;
        controller.Panels = new[] { new InGameUIController.PanelEntry { Kind = InGamePanel.Inventory, Root = inventory }, new InGameUIController.PanelEntry { Kind = InGamePanel.Craft, Root = craft }, new InGameUIController.PanelEntry { Kind = InGamePanel.Quest, Root = quests }, new InGameUIController.PanelEntry { Kind = InGamePanel.Dead, Root = dead } };
        foreach (var entry in controller.Panels) entry.Root.SetActive(false);
        controller.Tooltip = Rect("Tooltip", canvas.transform, new Vector2(320, 165), Vector2.zero); ((RectTransform)controller.Tooltip.transform).pivot = new Vector2(0, 1); Image(controller.Tooltip, Sprite("바탕-패널-쉐이딩.png"), Ink);
        controller.TooltipTitle = Text("Title", controller.Tooltip.transform, "", new Vector2(290, 34), new Vector2(160, -25), 22, TextAlignmentOptions.Left);
        controller.TooltipDescription = Text("Description", controller.Tooltip.transform, "", new Vector2(290, 105), new Vector2(160, -95), 17, TextAlignmentOptions.TopLeft); controller.Tooltip.SetActive(false);
        controller.Notice = Text("Notice", canvas.transform, "", new Vector2(900, 40), new Vector2(0, 170), 23);
        BuildMinimap(root.transform, hud.transform, display);
        BuildWorldLabels(canvas);
        TutorialTextUISetup.Configure(root);
        if (!scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<EventSystem>(true)).Any())
        {
            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); SceneManager.MoveGameObjectToScene(system, scene);
        }
        var gameplay = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GameplayIntegration>()).Single();
        if (gameplay.GetComponent<InGameUIBridge>() == null) gameplay.gameObject.AddComponent<InGameUIBridge>();
        InGameUILegacyLayout.Apply(root);
        TutorialTextUISetup.ConnectScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Built event-based InGame UI with reused sprites/prefabs, NotoSansKR, UI camera, and empty per-button effect fields";
    }
    static void BuildMinimap(Transform root, Transform hud, DisplayUIManager display)
    {
        var map = Rect("Minimap", hud, new Vector2(170, 170), new Vector2(-115, -110), new Vector2(1, 1));
        var raw = Rect("Map", map.transform, new Vector2(145, 145), Vector2.zero).AddComponent<RawImage>(); raw.raycastTarget = false;
        Image(Rect("Frame", map.transform, new Vector2(170, 170), Vector2.zero), Sprite("IngameUI/IngameImage/minimapframe.png"));
        var marker = Rect("Player", map.transform, new Vector2(12, 12), Vector2.zero); Image(marker, Sprite("IngameUI/Icons/cropped_circle_image.png"), Color.yellow); display.MinimapPlayer = (RectTransform)marker.transform;
        MinimapBaker.Configure(map.AddComponent<MinimapView>(), MinimapBaker.Bake());
    }
    static void BuildWorldLabels(Canvas canvas)
    {
        var hp = Rect("EnemyHpTemplate", null, new Vector2(105, 10), Vector2.zero); Image(hp, Sprite("IngameUI/IngameImage/HP_EnemyE.png"));
        var fill = Image(Rect("Fill", hp.transform, new Vector2(101, 8), Vector2.zero), Sprite("IngameUI/IngameImage/HP_Enemy.png")); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        var hpPrefab = PrefabUtility.SaveAsPrefabAsset(hp, Folder + "/EnemyHp.prefab"); UnityEngine.Object.DestroyImmediate(hp);
        var damage = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Existing + "DisplayUI/DamageText.prefab")); damage.name = "DamageText"; var label = damage.GetComponent<TMP_Text>(); label.font = font; label.fontSize = 32; label.color = new Color(1, .82f, .3f); label.raycastTarget = false;
        var damagePrefab = PrefabUtility.SaveAsPrefabAsset(damage, Folder + "/DamageText.prefab"); UnityEngine.Object.DestroyImmediate(damage);
        var layer = Rect("WorldLabels", canvas.transform, new Vector2(1920, 1080), Vector2.zero); ((RectTransform)layer.transform).anchorMin = Vector2.zero; ((RectTransform)layer.transform).anchorMax = Vector2.one; ((RectTransform)layer.transform).sizeDelta = Vector2.zero;
        layer.transform.SetAsFirstSibling(); var view = layer.AddComponent<EnemyHpUIManager>(); view.Canvas = canvas; view.HpPrefab = hpPrefab; view.DamagePrefab = damagePrefab;
    }
}
#endif
