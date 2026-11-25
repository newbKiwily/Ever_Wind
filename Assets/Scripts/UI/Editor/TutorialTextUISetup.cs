using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TutorialTextUISetup
{
    public static void Configure(GameObject root)
    {
        var canvas = root.GetComponentInChildren<Canvas>(true);
        var view = canvas.GetComponent<TextRenderManager>() ?? canvas.gameObject.AddComponent<TextRenderManager>();
        var panel = (canvas.transform.Find("DialoguePanel") ?? canvas.transform.Find("TutorialDialogue")) as RectTransform;
        if (panel == null)
        {
            panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
        }
        panel.name = "DialoguePanel";
        panel.gameObject.layer = canvas.gameObject.layer;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
        panel.anchoredPosition = new Vector2(0, -262); panel.sizeDelta = new Vector2(1500, 50);
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0, 0, 0, .4392157f); background.raycastTarget = false;
        background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = UnityEngine.UI.Image.Type.Sliced;
        var group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        foreach (string name in new[] { "Title", "Progress" })
        {
            var old = panel.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        var child = (panel.Find("Dialogue") ?? panel.Find("Instruction")) as RectTransform;
        if (child == null) { child = new GameObject("Dialogue", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>(); child.SetParent(panel, false); }
        child.name = "Dialogue";
        child.gameObject.layer = canvas.gameObject.layer;
        child.anchorMin = child.anchorMax = child.pivot = new Vector2(.5f, .5f);
        child.anchoredPosition = new Vector2(-45.7813f, 3); child.sizeDelta = new Vector2(1408.4373f, 50);
        var label = child.GetComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Noto_Sans_KR/static/NotoSansKR-Regular SDF.asset");
        label.fontSize = 36; label.enableAutoSizing = false; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.margin = new Vector4(0, 0, -91.92456f, 0);
        label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false; label.text = "";
        var serialized = new SerializedObject(label);
        serialized.FindProperty("m_HorizontalAlignment").intValue = 2;
        serialized.FindProperty("m_VerticalAlignment").intValue = 256;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        view.DialoguePanel = panel.gameObject; view.TextMeshProUGUI = label; view.CanvasGroup = group; view.FadeDuration = .5f;
        panel.gameObject.SetActive(false); EditorUtility.SetDirty(view);
    }

    public static void ConnectScene()
    {
        var view = Object.FindFirstObjectByType<TextRenderManager>(FindObjectsInactive.Include);
        foreach (var guide in Object.FindObjectsByType<TutorialGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            guide.TextRenderer = view;
            EditorUtility.SetDirty(guide);
            if (PrefabUtility.IsPartOfPrefabInstance(guide)) PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
        }
    }

    [MenuItem("Tools/EverWind/Connect Tutorial Text")]
    public static void Apply()
    {
        const string path = "Assets/Scripts/UI/InGame/Prefabs/InGameUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ConnectScene(); AssetDatabase.SaveAssets();
    }
}
