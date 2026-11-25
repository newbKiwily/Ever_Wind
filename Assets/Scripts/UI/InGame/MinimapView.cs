using UnityEngine;
public sealed class MinimapView : MonoBehaviour
{
    [System.Serializable]
    public struct MapImage
    {
        public int Id;
        public Texture2D Texture;
        public Vector2 Center;
        public float Radius;
    }

    public UnityEngine.UI.RawImage Image;
    public RectTransform Player;
    public MapImage[] Maps;
    // Older UI builders still supply this camera. The minimap now uses baked map art.
    public Camera MapCamera;
    void OnEnable()
    {
        if (MapCamera != null) MapCamera.enabled = false;
        PlayEvents.UIStateChanged += Apply;
        PlayEvents.EvUIStateRequested();
    }
    void OnDisable() { PlayEvents.UIStateChanged -= Apply; }
    void Apply(InGameUIState state)
    {
        if (MapCamera != null) MapCamera.enabled = false;
        if (Image == null || Player == null) return;
        MapImage selected = default;
        if (Maps != null) foreach (var map in Maps) if (map.Id == state.MapId) { selected = map; break; }
        bool visible = state.Ready && selected.Texture != null && selected.Radius > 0;
        Image.enabled = visible;
        Image.texture = visible ? selected.Texture : null;
        Player.gameObject.SetActive(visible);
        if (!visible) return;

        // +X is right, +Z is up. Use the exact same extent as the baked image.
        var rect = Image.rectTransform.rect;
        Vector2 uv = (new Vector2(state.PlayerPosition.x, state.PlayerPosition.z) - selected.Center) / (2 * selected.Radius) + Vector2.one * .5f;
        Vector3 position = Image.rectTransform.TransformPoint(new Vector3(Mathf.LerpUnclamped(rect.xMin, rect.xMax, uv.x), Mathf.LerpUnclamped(rect.yMin, rect.yMax, uv.y), 0));
        Vector3 local = Player.parent.InverseTransformPoint(position);
        var min = Player.parent.InverseTransformPoint(Image.rectTransform.TransformPoint(rect.min));
        var max = Player.parent.InverseTransformPoint(Image.rectTransform.TransformPoint(rect.max));
        Vector2 half = Vector2.Scale(Player.rect.size, new Vector2(Mathf.Abs(Player.localScale.x), Mathf.Abs(Player.localScale.y))) * .5f;
        local.x = Mathf.Clamp(local.x, min.x + half.x, max.x - half.x);
        local.y = Mathf.Clamp(local.y, min.y + half.y, max.y - half.y);
        Player.localPosition = local;
    }
}
