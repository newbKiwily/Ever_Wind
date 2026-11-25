using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor-only cartography. Server map coordinates are baked into ordinary textures;
// the client does not read server files or introduce another map-data receiver.
public static class MinimapBaker
{
    public const string Folder = "Assets/Scripts/GamePlayEdit/Maps/Minimap";
    const int Size = 1024;
    const string UiPath = "Assets/Scripts/UI/InGame/Prefabs/InGameUI.prefab";
    static readonly Color Ground = new Color32(133, 153, 99, 255);
    static readonly Color Forest = new Color32(47, 83, 57, 255);
    static readonly Color Rock = new Color32(115, 124, 112, 255);
    static readonly Color Building = new Color32(151, 101, 63, 255);
    static readonly Color Road = new Color32(209, 192, 139, 255);
    static readonly Color Ink = new Color32(48, 62, 45, 255);

    sealed class Chart
    {
        public int Id, Cols, Rows;
        public float Radius, Step;
        public Vector2 Center, Origin;
        public float[] Heights;
        public readonly List<Vector4> Boxes = new List<Vector4>();
        public readonly List<Vector3> Resources = new List<Vector3>();
        public Color[] Pixels = new Color[Size * Size];
        public Vector2 Pixel(float x, float z) => (new Vector2(x, z) - Center) * ((Size - 1) / (2 * Radius)) + Vector2.one * ((Size - 1) * .5f);
        public float Height(float x, float z)
        {
            int col = Mathf.Clamp(Mathf.RoundToInt((x - Origin.x) / Step), 0, Cols - 1);
            int row = Mathf.Clamp(Mathf.RoundToInt((z - Origin.y) / Step), 0, Rows - 1);
            return Heights[row * Cols + col];
        }
        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) return;
            Pixels[y * Size + x] = Color.Lerp(Pixels[y * Size + x], c, c.a);
        }
        public void Ellipse(Vector2 p, Vector2 r, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(p.y - r.y)); y <= Mathf.Min(Size - 1, Mathf.CeilToInt(p.y + r.y)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(p.x - r.x)); x <= Mathf.Min(Size - 1, Mathf.CeilToInt(p.x + r.x)); x++)
                    if (new Vector2((x - p.x) / r.x, (y - p.y) / r.y).sqrMagnitude <= 1) Blend(x, y, c);
        }
        public void Rect(Vector2 p, Vector2 r, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(p.y - r.y)); y <= Mathf.Min(Size - 1, Mathf.CeilToInt(p.y + r.y)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(p.x - r.x)); x <= Mathf.Min(Size - 1, Mathf.CeilToInt(p.x + r.x)); x++) Blend(x, y, c);
        }
    }

    static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    static Chart Read(int id)
    {
        string file = $"EverWindServer/LoginServer/Content/MapData/Maps/Map{id}.txt";
        var chart = new Chart { Id = id };
        foreach (string line in File.ReadLines(file))
        {
            var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 0 || p[0].StartsWith("#")) continue;
            switch (p[0])
            {
                case "map": chart.Radius = F(p[2]); break;
                case "spawn": chart.Center = new Vector2(F(p[5]), F(p[6])); break;
                case "box": chart.Boxes.Add(new Vector4(F(p[3]), F(p[4]), F(p[5]), F(p[6]))); break;
                case "resourcespawn": chart.Resources.Add(new Vector3(F(p[4]), F(p[5]), F(p[3]))); break;
                case "surface":
                    chart.Origin = new Vector2(F(p[2]), F(p[3])); chart.Step = F(p[4]); chart.Cols = int.Parse(p[5]); chart.Rows = int.Parse(p[6]);
                    byte[] bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(file), p[7]));
                    if (bytes.Length != chart.Cols * chart.Rows * sizeof(float)) throw new InvalidDataException("Invalid map height grid: " + file);
                    chart.Heights = new float[chart.Cols * chart.Rows]; Buffer.BlockCopy(bytes, 0, chart.Heights, 0, bytes.Length);
                    break;
            }
        }
        if (chart.Radius <= 0 || chart.Heights == null) throw new InvalidDataException(file);
        return chart;
    }

    static string Hierarchy(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s.ToLowerInvariant();
    }

    static void Terrain(Chart c)
    {
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
        {
            Vector2 world = c.Center + (new Vector2(x, y) / (Size - 1) - Vector2.one * .5f) * (2 * c.Radius);
            float h = c.Height(world.x, world.y);
            float dx = c.Height(world.x + 1, world.y) - c.Height(world.x - 1, world.y);
            float dz = c.Height(world.x, world.y + 1) - c.Height(world.x, world.y - 1);
            float shade = Mathf.Clamp((dz - dx) * .022f, -.14f, .12f);
            Color color = Ground * (1 + shade);
            float contour = Mathf.Repeat(h, 5);
            if (contour < .14f) color = Color.Lerp(color, Ink, .13f);
            if ((world - c.Center).sqrMagnitude > c.Radius * c.Radius) color = Color.Lerp(color, Ink, .48f);
            color.a = 1; c.Pixels[y * Size + x] = color;
        }
    }

    // Project real path/sand mesh triangles rather than inventing connections.
    static void Paths(Chart c, GameObject prefab)
    {
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
        {
            string name = Hierarchy(filter.transform);
            if (!name.Contains("/path/") && !name.Contains("sand") && !name.Contains("water")) continue;
            var mesh = filter.sharedMesh;
            if (mesh == null) continue;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var points = vertices.Select(v => { var w = filter.transform.TransformPoint(v); return c.Pixel(w.x, w.z); }).ToArray();
            Color color = name.Contains("water") ? new Color32(95, 150, 159, 255) : Road;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 a = points[triangles[i]], b = points[triangles[i + 1]], d = points[triangles[i + 2]];
                float cross = Cross(b - a, d - a);
                if (Mathf.Abs(cross) < .01f) continue;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, d.x))), x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, d.x)));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, d.y))), y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, d.y)));
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x, y);
                    float u = Cross(b - p, d - p) / cross, v = Cross(d - p, a - p) / cross;
                    if (u >= 0 && v >= 0 && u + v <= 1) c.Blend(x, y, color);
                }
            }
        }
    }
    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    static void Objects(Chart c, GameObject prefab)
    {
        // Server boxes describe XZ footprints, while visual prefabs may use separate
        // collision proxies. Match the footprint to the rendered static model.
        var models = prefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.enabled && !Hierarchy(x.transform).Contains("/terrain/") && !Hierarchy(x.transform).Contains("/path/")).ToArray();
        float scale = (Size - 1) / (2 * c.Radius);
        foreach (var b in c.Boxes.OrderByDescending(b => b.w * b.z))
        {
            var near = models.OrderBy(x => (new Vector2(x.bounds.center.x, x.bounds.center.z) - new Vector2(b.x, b.y)).sqrMagnitude + (new Vector2(x.bounds.extents.x, x.bounds.extents.z) - new Vector2(b.z, b.w)).sqrMagnitude).FirstOrDefault();
            string name = near != null ? Hierarchy(near.transform) : "";
            bool tree = name.Contains("tree") || name.Contains("bush") || name.Contains("vegetation");
            bool building = name.Contains("building") || name.Contains("house") || name.Contains("fence") || name.Contains("exterior") || name.Contains("wall");
            Vector2 p = c.Pixel(b.x, b.y), r = new Vector2(Mathf.Max(3, b.z * scale), Mathf.Max(3, b.w * scale));
            Color fill = tree ? Forest : building ? Building : Rock;
            if (tree)
            {
                c.Ellipse(p + new Vector2(2, -3), r + Vector2.one * 2, new Color(0.12f, .18f, .10f, .45f));
                c.Ellipse(p, r + Vector2.one, Ink);
                c.Ellipse(p, r, fill);
                c.Ellipse(p + new Vector2(-r.x * .2f, r.y * .2f), r * .65f, Color.Lerp(fill, Ground, .3f));
            }
            else
            {
                c.Rect(p + new Vector2(2, -2), r + Vector2.one, Ink);
                c.Rect(p, r, fill);
                c.Rect(p + new Vector2(0, r.y * .5f), new Vector2(r.x, Mathf.Max(1, r.y * .18f)), Color.Lerp(fill, Road, .3f));
            }
        }
        foreach (var resource in c.Resources)
        {
            var p = c.Pixel(resource.x, resource.y);
            Color fill = resource.z == 3 ? Rock : Forest;
            float radius = resource.z == 1 ? 5 : 4;
            c.Ellipse(p, Vector2.one * (radius + 1), Ink);
            c.Ellipse(p, Vector2.one * radius, fill);
        }
        // North glyph, inside the frame. No fabricated POIs or monster positions.
        int ox = Size / 2 - 10, oy = Size - 48;
        string[] glyph = { "10001", "11001", "10101", "10011", "10001" };
        for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) if (glyph[4 - y][x] == '1')
            c.Rect(new Vector2(ox + x * 4, oy + y * 4), Vector2.one * 1.5f, new Color32(241, 231, 186, 255));
    }

    public static MinimapView.MapImage[] Bake()
    {
        var maps = new List<MinimapView.MapImage>();
        for (int id = 0; id < 2; id++)
        {
            var c = Read(id);
            var prefab = PrefabUtility.LoadPrefabContents($"Assets/Scripts/GamePlayEdit/Maps/Map{id}/Map{id}Visual.prefab");
            try { Terrain(c); Paths(c, prefab); Objects(c, prefab); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var texture = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            texture.SetPixels(c.Pixels); texture.Apply();
            string path = Folder + $"/Map{id}Minimap.png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = Size;
            importer.SaveAndReimport();
            maps.Add(new MinimapView.MapImage { Id = id, Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path), Center = c.Center, Radius = c.Radius });
        }
        return maps.ToArray();
    }

    public static void Configure(MinimapView view, MinimapView.MapImage[] maps)
    {
        view.Maps = maps;
        view.Image = view.transform.Find("Map").GetComponent<UnityEngine.UI.RawImage>();
        view.Player = (RectTransform)view.transform.Find("Player");
        var frame = (RectTransform)view.transform.Find("Frame");
        // Artwork has its frame at 5..95% of the image. Fill the opening behind it.
        view.Image.rectTransform.sizeDelta = Vector2.Scale(frame.rect.size, new Vector2(Mathf.Abs(frame.localScale.x), Mathf.Abs(frame.localScale.y))) * .9f;
        view.Image.rectTransform.anchoredPosition = frame.anchoredPosition;
        view.Image.texture = maps[0].Texture; view.Image.color = Color.white; view.Image.raycastTarget = false;
        view.Image.uvRect = new Rect(0, 0, 1, 1);
        view.Player.SetAsLastSibling();
        if (view.MapCamera != null) view.MapCamera.enabled = false;
        EditorUtility.SetDirty(view);
    }

    [MenuItem("Tools/EverWind/Rebuild Minimap Art")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before baking minimaps.");
        var maps = Bake();
        var root = PrefabUtility.LoadPrefabContents(UiPath);
        try
        {
            Configure(root.GetComponentInChildren<MinimapView>(true), maps);
            PrefabUtility.SaveAsPrefabAsset(root, UiPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var view in UnityEngine.Object.FindObjectsByType<MinimapView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Configure(view, maps);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view.Image.rectTransform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
    }
}
