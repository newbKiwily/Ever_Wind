#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// origin/main: Everwind/Assets/Scenes/InGame.unity, 1920 x 1080.
// Keep the current event views; restore the original artwork and RectTransforms.
public static class InGameUILegacyLayout
{
    const string Folder = "Assets/Scripts/UI/InGame/Prefabs/";
    const string Art = "Assets/UIImage/";
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path.StartsWith("Assets/") ? path : Art + path);
    static RectTransform Place(Transform parent, string name, float width, float height, float x, float y, Vector2? anchor = null)
    {
        var child = parent.Find(name);
        var rect = child != null ? (RectTransform)child : (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false); rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f); rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y);
        return rect;
    }
    static Image Skin(Transform rect, string path, Color? tint = null)
    {
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = path == null ? null : Sprite(path); image.color = tint ?? Color.white;
        image.raycastTarget = rect.GetComponent<Button>() != null;
        return image;
    }
    static TMP_Text Label(Transform parent, string name, string text, float width, float height, float x, float y, float size)
    {
        var rect = Place(parent, name, width, height, x, y);
        var label = rect.GetComponent<TMP_Text>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset");
        label.text = text; label.fontSize = size; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    static void Grid(Transform scrollRoot, float width, float height, float contentWidth, Vector2 cell, int columns, Vector2 spacing, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var scroll = scrollRoot.GetComponent<ScrollRect>();
        Place(scrollRoot, "Viewport", width, height, 0, 0);
        var content = scroll.content; content.anchorMin = content.anchorMax = new Vector2(.5f, 1);
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        var grid = content.GetComponent<GridLayoutGroup>(); grid.cellSize = cell;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns;
        grid.spacing = spacing; grid.padding = new RectOffset(); grid.childAlignment = alignment;
    }
    static void FullPanel(Transform panel)
    {
        Place(panel.parent, panel.name, 1920, 1080, 0, 0);
        Skin(panel, "제작-배경.png").raycastTarget = true;
        panel.Find("Title").gameObject.SetActive(false);
        Place(panel, "Close", 44, 38, 928, 510).SetAsLastSibling();
    }
    static void Templates()
    {
        foreach (string name in new[] { "ItemSlot", "EquipmentSlot", "CraftItemSlot", "IngredientSlot", "QuestContent" })
        {
            string path = Folder + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var slot = root.GetComponent<Slot>();
                var rect = (RectTransform)root.transform;
                var size = name == "CraftItemSlot" ? new Vector2(760, 200) : name == "QuestContent" ? new Vector2(240, 100) : name == "ItemSlot" ? new Vector2(200, 200) : name == "EquipmentSlot" ? new Vector2(150, 150) : new Vector2(130, 130);
                rect.sizeDelta = size;
                Skin(rect, name == "CraftItemSlot" ? "제작슬롯바탕패널.png" : name == "ItemSlot" ? "inventorypanel.png" : name == "QuestContent" ? "바탕-패널-쉐이딩.png" : null,
                    name == "EquipmentSlot" || name == "IngredientSlot" ? Color.clear : Color.white);
                if (name == "CraftItemSlot")
                {
                    Skin(Place(rect, "ItemImagePanel", 150, 150, -259, 0), "slot.png");
                    slot.ItemImage.transform.SetParent(rect.Find("ItemImagePanel"), false);
                    Place(rect.Find("ItemImagePanel"), slot.ItemImage.name, 120, 120, 0, 0);
                    Place(rect, slot.NameText.name, 310, 50, -25, 45); slot.NameText.fontSize = 28;
                    slot.CountText.gameObject.SetActive(false);
                    Skin(Place(rect, "Select", 180, 60, 261, 0), "버튼.png");
                    Label(rect.Find("Select"), "Label", "선택", 170, 50, 0, 0, 27);
                }
                else if (name == "QuestContent")
                {
                    Place(rect, slot.NameText.name, 224, 30, 0, 30); slot.NameText.fontSize = 19;
                }
                else
                {
                    float icon = name == "ItemSlot" ? 150 : name == "EquipmentSlot" ? 120 : 110;
                    Place(rect, slot.ItemImage.name, icon, icon, 0, 0);
                    Place(rect, slot.NameText.name, size.x, 26, 0, -size.y / 2 - 12); slot.NameText.fontSize = 18;
                    slot.NameText.gameObject.SetActive(false);
                    slot.CountText.gameObject.SetActive(name != "EquipmentSlot");
                    Place(rect, slot.CountText.name, size.x - 8, name == "IngredientSlot" ? 50 : 30, 0, name == "IngredientSlot" ? -84 : -size.y / 2 + 20);
                    slot.CountText.fontSize = name == "ItemSlot" ? 26 : 18;
                    slot.CountText.alignment = name == "ItemSlot" ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
    public static void Apply(GameObject root)
    {
        Templates();
        var canvas = root.transform.Find("Canvas");
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
        var hud = canvas.Find("HUD");
        var vitals = Place(hud, "Vitals", 0, 0, 0, 0, new Vector2(0, 1));
        Skin(vitals, null, Color.clear);
        Skin(Place(vitals, "ProfileBackground", 130, 130, 96.79944f, -100), "IngameUI/profilebackground.png");
        var maskedPortrait = vitals.Find("ProfileMask/Profile");
        if (maskedPortrait != null) maskedPortrait.SetParent(vitals, false);
        Skin(Place(vitals, "Profile", 90, 90, 98.29944f, -101), "IngameUI/NormalProfile.png");
        Skin(Place(vitals, "ProfileFrame", 130, 130, 99.59949f, -101.59998f), "IngameUI/profile2.png");
        Skin(Place(vitals, "HealthBackground", 212.1f, 25.18355f, 250.1f, -134.9875f), "IngameUI/IngameImage/HP_Emty.png");
        Skin(Place(vitals, "HealthFill", 212.1f, 24.5f, 250.1f, -134.9875f), "IngameUI/IngameImage/HP.png");
        Skin(Place(vitals, "HealthFrame", 212.1f, 28, 250.1f, -135.2675f), "IngameUI/IngameImage/HP_UI.png");
        Place(vitals, "Health", 212, 26, 250.1f, -135).SetAsLastSibling();
        Place(vitals, "Growth", 240, 26, 250, -100);
        var skills = Place(hud, "Skills", 420, 255, -300, 202.5f, new Vector2(1, 0));
        for (int i = 0; i < 5; ++i)
        {
            float x = i < 3 ? (i - 1) * 150 : (i - 3.5f) * 150;
            var skill = Place(skills, "Skill " + (i + 1), 120, 120, x, i < 3 ? 67.5f : -67.5f);
            Skin(skill, "IngameUI/IngameImage/Skill.png");
            Place(skill, "Icon", 80, 80, 0, 0);
            if (i < 3) Skin(skill.Find("Icon"), "IngameUI/Icons/cropped_circle_image" + (i == 0 ? "" : " (" + i + ")") + ".png");
            Place(skill, "Cooldown", 120, 120, 0, 0);
            Skin(Place(skill, "NumberFrame", 150, 150, 2, -11), "IngameUI/IngameImage/Skill_Num.png").transform.SetAsLastSibling();
            var label = Place(skill, "Label", 25, 25, -39.7f, 42.3f); label.GetComponent<TMP_Text>().fontSize = 20; label.SetAsLastSibling();
        }
        var menu = Place(hud, "Menu", 100, 100, -378, -81, Vector2.one);
        string[] ids = { "quest", "craft", "inventory", "logout" };
        string[] icons = { "T_1_scroll_.png", "T_14_wrench_.png", "T_4_coin_bag1_.png", "Update/bonus_03.png" };
        string[] keys = { "Q", "C", "I", "ESC" };
        for (int i = 0; i < ids.Length; ++i)
        {
            var button = Place(menu, ids[i], 80, 80, -225 + i * 85, 4);
            Skin(button, "IngameUI/IngameImage/Skill.png");
            Skin(Place(button, "MenuIcon", 60, 60, 0, 0), "Assets/Clean Vector Icons/" + icons[i]);
            Skin(Place(button, "NumberFrame", 150, 150, 9.9f, -16.4f), "IngameUI/IngameImage/Skill_Num.png");
            Label(button, "Label", keys[i], 25, 25, -31.8f, 36.9f, i == 3 ? 9 : 20).transform.SetAsLastSibling();
        }
        var map = Place(hud, "Minimap", 80, 80, -156, -163, Vector2.one);
        Place(map, "Map", 80, 80, 0, 0); Place(map, "Frame", 93, 93, 0, 0); Place(map, "Player", 5.5f, 5.5f, 0, 0);
        var minimap = map.GetComponent<MinimapView>();
        if (minimap != null && minimap.Maps != null && minimap.Maps.Length > 0) MinimapBaker.Configure(minimap, minimap.Maps);
        var inventory = canvas.Find("Inventory"); FullPanel(inventory);
        var items = Place(inventory, "Items", 901, 1024, 467.5f, 0); Skin(items, "바탕패널.png");
        Grid(items, 901, 1024, 850, new Vector2(200, 200), 4, new Vector2(10, 10));
        var equipment = Place(inventory, "Equipment", 900, 1024, -474, -.9328f); Skin(equipment, "equipUI.png");
        var slots = Place(equipment, "Slots", 150, 940, 325, 0);
        Grid(slots, 150, 940, 150, new Vector2(150, 150), 1, new Vector2(0, 47));
        // The original equipment artwork contains these five slot backgrounds.
        var craft = canvas.Find("Craft"); FullPanel(craft);
        var space = Place(craft, "CraftSpace", 901, 1024, -468.8235f, 2.5f); Skin(space, "바탕패널.png"); space.SetAsFirstSibling();
        foreach (string name in new[] { "Ingredients", "SelectedRecipe", "Craft" })
            if (craft.Find(name) != null && space.Find(name) != null) UnityEngine.Object.DestroyImmediate(space.Find(name).gameObject);
        var recipes = Place(craft, "Recipes", 901, 1024, 477.5f, 1.5f); Skin(recipes, "바탕패널.png");
        Grid(recipes, 901, 1024, 780, new Vector2(760, 200), 1, new Vector2(0, 20));
        var ingredients = craft.Find("Ingredients") ?? space.Find("Ingredients"); ingredients.SetParent(space, false);
        Place(space, "Ingredients", 901, 279.2416f, 0, -150.06363f); Skin(ingredients, "제작슬롯바탕패널.png");
        Grid(ingredients, 901, 279.2416f, 901, new Vector2(130, 130), 5, new Vector2(30, 30), TextAnchor.UpperCenter);
        ingredients.GetComponent<ScrollRect>().content.GetComponent<GridLayoutGroup>().padding.top = 75;
        var selectedName = craft.Find("SelectedRecipe") ?? space.Find("SelectedRecipe"); selectedName.SetParent(space, false); Place(space, "SelectedRecipe", 600, 50, 0, 123);
        Label(space, "IngredientTitle", "필요 재료", 300, 80, 0, -41, 30).transform.SetAsLastSibling();
        var result = Place(space, "Result", 210, 210, 0, 285); Skin(result, "slot.png");
        craft.GetComponent<CraftUI>().SelectedIcon = Skin(Place(result, "Icon", 175, 175, 0, 0), null);
        var execute = craft.Find("Craft") ?? space.Find("Craft"); execute.SetParent(space, false); Place(space, "Craft", 240, 75, 0, -391); Skin(execute, "버튼.png");
        Place(execute, "Label", 230, 65, 0, 0).GetComponent<TMP_Text>().fontSize = 30;
        var quests = Place(canvas, "Quests", 254.2663f, 200, -157.87f, -416, Vector2.one);
        Skin(quests, "바탕패널.png", new Color(1, 1, 1, .392f)).raycastTarget = true;
        quests.Find("Title").gameObject.SetActive(false);
        Place(quests, "Close", 26, 26, 112, 115); Place(quests.Find("Close"), "Label", 26, 26, 0, 0);
        var questList = Place(quests, "QuestList", 254, 200, 0, 0); Grid(questList, 254, 200, 240, new Vector2(240, 100), 1, new Vector2(0, 20));
        var dead = Place(canvas, "Dead", 1920, 1080, 0, 0); Skin(dead, "LogIn/deadpanel3.png").raycastTarget = true;
        Place(dead, "Title", 600, 70, 0, 230);
        var selection = Place(dead, "SelectPanel", 300, 250, 0, -182);
        Skin(selection, "LogIn/error_panel.png", new Color(.8207547f, .80914026f, .80914026f));
        Skin(Place(selection, "Logo", 70, 60, -107, 90), "IngameUI/IngameImage/Logo_Login.png", new Color(1, 1, 1, .6745098f));
        var remaining = dead.Find("Remaining") ?? selection.Find("Remaining"); remaining.SetParent(selection, false);
        Place(selection, "Remaining", 210, 60, 30, 82).GetComponent<TMP_Text>().fontSize = 18;
        var revive = dead.Find("Revive") ?? selection.Find("Revive"); revive.SetParent(selection, false);
        Place(selection, "Revive", 250, 60, 0, 0); Skin(revive, "LogIn/robby_button.png", new Color(.8868f, .8868f, .8868f));
        var exit = Place(selection, "Logout", 250, 60, 0, -68.1f);
        var exitImage = Skin(exit, "LogIn/robby_button.png", new Color(.8868f, .8868f, .8868f));
        var exitButton = exit.GetComponent<Button>() ?? exit.gameObject.AddComponent<Button>(); exitButton.targetGraphic = exitImage; exitImage.raycastTarget = true;
        var exitControl = exit.GetComponent<UIControlButton>() ?? exit.gameObject.AddComponent<UIControlButton>(); exitControl.ControlId = "dead:logout"; exitControl.Command = UICommandKind.Logout;
        Label(exit, "Label", "로그아웃", 230, 50, 0, 0, 23);
        dead.Find("Close").gameObject.SetActive(false);
        var tooltip = canvas.Find("Tooltip");
        var tooltipRect = Place(canvas, "Tooltip", 500, 300, 0, 0); tooltipRect.pivot = new Vector2(0, 1);
        Skin(tooltip, null, new Color(.0754717f, .052450456f, .052450456f, .60784316f));
        Place(tooltip, "Title", 451.5016f, 50, 252.75f, -44, new Vector2(0, 1));
        Place(tooltip, "Description", 450, 190, 250, -175, new Vector2(0, 1));
        InGameUIPortraitAssets.Apply(canvas);
        InGameUICharacterPreviewSetup.ApplyUI(canvas);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/EverWind/Restore main branch UI layout")]
    public static string ApplyExisting()
    {
        if (Application.isPlaying) throw new InvalidOperationException("플레이 모드를 먼저 종료하세요.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        if (scene.isDirty) throw new InvalidOperationException("기존 미저장 변경을 먼저 저장하세요.");
        var root = scene.GetRootGameObjects().Single(go => go.name == "InGame UI");
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        Apply(root);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, Folder + "InGameUI.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Restored main branch UI artwork/layout; five skills arranged 3 above / 2 below.";
    }
}
#endif
