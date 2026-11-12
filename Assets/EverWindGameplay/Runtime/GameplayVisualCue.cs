using UnityEngine;
using UnityEngine.UI;

namespace EverWind.Gameplay
{
// Presentation only: neither cue changes HP, action completion, or inventory.
public sealed class GameplayVisualCue : MonoBehaviour
{
    float age, lifetime;
    Text text;
    LineRenderer line;
    Material ownedMaterial;
    public static void Swing(Transform actor)
    {
        var root = new GameObject("Attack visual cue");
        root.transform.SetPositionAndRotation(actor.position + Vector3.up, actor.rotation);
        var cue = root.AddComponent<GameplayVisualCue>();
        cue.lifetime = .25f;
        cue.line = root.AddComponent<LineRenderer>();
        cue.line.useWorldSpace = false;
        cue.line.widthMultiplier = .07f;
        cue.line.positionCount = 24;
        cue.ownedMaterial = new Material(Shader.Find("Sprites/Default"));
        cue.line.sharedMaterial = cue.ownedMaterial;
        for (int i = 0; i < 24; ++i)
        {
            float angle = Mathf.Lerp(-1.2f, 1.2f, i / 23f);
            cue.line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 1.2f);
        }
    }
    public static void Damage(Vector3 position, float amount)
    {
        var root = new GameObject("Server damage number", typeof(RectTransform), typeof(Canvas));
        root.transform.position = position;
        root.transform.localScale = Vector3.one * .012f;
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 80);
        var label = new GameObject("Damage", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(root.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var cue = root.AddComponent<GameplayVisualCue>();
        cue.lifetime = .8f;
        cue.text = label.GetComponent<Text>();
        cue.text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cue.text.fontSize = 48;
        cue.text.alignment = TextAnchor.MiddleCenter;
        cue.text.text = amount.ToString("0");
        cue.text.raycastTarget = false;
    }
    void Update()
    {
        age += Time.unscaledDeltaTime;
        float alpha = Mathf.Clamp01(1 - age / lifetime);
        if (text != null)
        {
            transform.position += Vector3.up * Time.unscaledDeltaTime;
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
            text.color = new Color(1, .8f, .2f, alpha);
        }
        if (line != null)
            line.startColor = line.endColor = new Color(.7f, .95f, 1, alpha);
        if (age >= lifetime)
            Destroy(gameObject);
    }
    void OnDestroy()
    {
        if (ownedMaterial != null)
            Destroy(ownedMaterial);
    }
}
}
