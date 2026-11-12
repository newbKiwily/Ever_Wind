using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EverWind.Gameplay
{
// Small verification HUD using the project's existing uGUI system.
public sealed class GameplayHud : MonoBehaviour
{
    public ServerTransceiverManager Registry;
    InputField user, password, host;
    Text status;
    Font font;
    GameObject loginPanel;
    Image progress;
    int recipeIndex;
    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas =
            new GameObject("GameplayHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.transform.SetParent(transform);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        loginPanel = Panel(canvas.transform, "Login", new Vector2(20, -20), new Vector2(340, 210));
        Label(loginPanel.transform, "EverWind / Action & State", new Vector2(12, -12), new Vector2(315, 25),
              18);
        host = Field(loginPanel.transform, "127.0.0.1", 42);
        user = Field(loginPanel.transform, "Account", 78);
        password = Field(loginPanel.transform, "Password", 114);
        password.contentType = InputField.ContentType.Password;
        Button(loginPanel.transform, "Connect", new Vector2(12, -156),
               () =>
               {
                   if (!System.Uri.TryCreate("tcp://" + host.text.Trim(), System.UriKind.Absolute,
                                             out var endpoint))
                       return;
                   Registry.Transport.Host = endpoint.Host;
                   Registry.Transport.Port = endpoint.Port > 0 ? endpoint.Port : 4000;
                   Registry.Transport.Login(user.text, password.text);
               });
        var info = Panel(canvas.transform, "Status", new Vector2(20, -245), new Vector2(390, 235));
        status = Label(info.transform, "", new Vector2(12, -10), new Vector2(365, 215), 15);
        progress = Panel(canvas.transform, "Action progress", new Vector2(440, -660), new Vector2(400, 18))
                       .GetComponent<Image>();
        progress.color = new Color(.2f, .8f, .6f);
        var controls = Panel(canvas.transform, "Controls", new Vector2(20, -500), new Vector2(390, 180));
        Label(controls.transform,
              "WASD Move / RMB Orbit / Wheel Zoom\nLMB Combat / E Next target / 1-2 Attack\nSpace Gather / " +
              "C Craft / Esc " +
                  "Cancel",
              new Vector2(12, -10), new Vector2(370, 65), 14);
        Button(controls.transform, "Map 1", new Vector2(12, -80), () => Map(1));
        Button(controls.transform, "Map 2", new Vector2(140, -80), () => Map(2));
        Button(controls.transform, "Revive", new Vector2(12, -123),
               () =>
               { Registry.LocalActor?.Send(new Request { Kind = RequestKind.Revive }); });
        Button(controls.transform, "Next recipe", new Vector2(140, -123),
               () =>
               {
                   if (Registry.Catalog != null && Registry.Catalog.Recipes.Length > 0)
                       recipeIndex = (recipeIndex + 1) % Registry.Catalog.Recipes.Length;
               });
        Button(controls.transform, "Craft", new Vector2(268, -123),
               () =>
               {
                   if (Registry.LocalActor != null && Registry.Catalog != null &&
                       Registry.Catalog.Recipes.Length > 0)
                       Registry.LocalActor.GetComponent<PlayerActionController>().RequestCraft(
                           Registry.Catalog.Recipes[recipeIndex].Id);
               });
    }
    void Map(int map)
    {
        if (Registry.LocalActor != null)
            Registry.LocalActor.Send(new Request { Kind = RequestKind.ChangeMap, Definition = map });
    }
    void Update()
    {
        if (status == null)
            return;
        var a = Registry.LocalActor;
        var controller = a != null ? a.GetComponent<PlayerActionController>() : null;
        var presentation = a != null ? a.GetComponent<EntityPresentation>() : null;
        progress.gameObject.SetActive(presentation != null && presentation.HasAction);
        progress.fillAmount = presentation != null ? presentation.Progress : 0;
        progress.rectTransform.sizeDelta = new Vector2(400 * progress.fillAmount, 18);
        status.text = Registry.Transport.Status;
        if (a != null)
        {
            status.text +=
                $"\nEntity {a.EntityId} | {a.State.Current.Mode}\nHP {a.Hp:0}/{a.MaxHp:0} | Action {a.ActionId}\nTarget: {(controller!=null&&controller.SelectedTarget!=null?controller.SelectedTarget.EntityId:0)}\n{controller?.LastResult}\n{Registry.ZoneStatus}\nInventory:";
            foreach (var pair in Registry.Inventory)
                status.text +=
                    $" {(Registry.Catalog!=null?Registry.Catalog.ItemName(pair.Key):pair.Key.ToString())}×{pair.Value}";
            if (Registry.Catalog != null && Registry.Catalog.Recipes.Length > 0)
                status.text += "\nRecipe: " + Registry.Catalog.Recipes[recipeIndex].Definition.ResultItemName;
        }
        loginPanel.SetActive(!Registry.Transport.Ready);
    }
    GameObject Panel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(.035f, .055f, .08f, .92f);
        return go;
    }
    Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.text = text;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
    InputField Field(Transform parent, string placeholder, float y)
    {
        var go = Panel(parent, "Input", new Vector2(12, -y), new Vector2(315, 30));
        go.GetComponent<Image>().color = new Color(.16f, .2f, .25f);
        var t = Label(go.transform, "", new Vector2(7, -4), new Vector2(300, 25), 15);
        var p = Label(go.transform, placeholder, new Vector2(7, -4), new Vector2(300, 25), 15);
        p.color = Color.gray;
        var input = go.AddComponent<InputField>();
        input.textComponent = t;
        input.placeholder = p;
        if (placeholder == "127.0.0.1")
            input.text = placeholder;
        return input;
    }
    void Button(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = Panel(parent, text, pos, new Vector2(116, 32));
        go.GetComponent<Image>().color = new Color(.12f, .35f, .4f);
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        b.onClick.AddListener(onClick);
        Label(go.transform, text, new Vector2(10, -5), new Vector2(100, 25), 15);
    }
}
}
