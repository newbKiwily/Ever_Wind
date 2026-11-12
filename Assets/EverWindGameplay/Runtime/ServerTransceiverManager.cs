using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class ServerTransceiverManager : MonoBehaviour
{
    public NetworkClient Transport;
    public GameplayCatalog Catalog;
    public GameplayWorldCatalog WorldCatalog;
    public LevelDefinition CurrentLevel { get; private set; }
    public int CurrentMap { get; private set; }
    public Vector3 MapCenter { get; private set; }
    public bool DebugGeometry;
    GameObject levelVisual;
    public GameObject PlayerVisual, ResourceVisual, WallVisual;
    public GameObject TargetIndicatorVisual;
    public GameObject[] MonsterVisuals;
    public ActionVisualProfile PlayerProfile;
    public ActionVisualProfile[] MonsterProfiles;
    public float MapRadius { get; private set; } = 24;
    public IReadOnlyDictionary<int, int> Inventory => inventory;
    public IEnumerable<ServerTransceiver> Entities => entities.Values;
    public string ZoneStatus { get; private set; } = "Outside event zone";
    readonly Dictionary<int, ServerTransceiver> entities = new Dictionary<int, ServerTransceiver>();
    readonly Dictionary<int, int> inventory = new Dictionary<int, int>();
    public ServerTransceiver LocalActor =>
        Transport != null && entities.TryGetValue(Transport.LocalEntityId, out var a) ? a : null;
    public float SkillReach(int id) => Catalog != null ? Catalog.Reach(id) : 0;
    void OnEnable()
    {
        if (Transport == null && GameplaySession.Current != null)
            Transport = GameplaySession.Current.Transport;
        if (Transport == null)
            return;
        Transport.Message += Receive;
        Transport.Disconnected += Clear;
    }
    void OnDisable()
    {
        if (Transport != null)
        {
            Transport.Message -= Receive;
            Transport.Disconnected -= Clear;
        }
        Clear();
    }
    public void Register(ServerTransceiver actor)
    {
        entities[actor.EntityId] = actor;
    }
    public void Unregister(ServerTransceiver actor)
    {
        if (entities.TryGetValue(actor.EntityId, out var current) && current == actor)
            entities.Remove(actor.EntityId);
    }
    public bool TryGet(int id,
                       out ServerTransceiver actor) => entities.TryGetValue(id, out actor) && actor != null;
    void Clear()
    {
        var values = new List<ServerTransceiver>(entities.Values);
        entities.Clear();
        foreach (var a in values)
            if (a != null)
                Destroy(a.gameObject);
        inventory.Clear();
        if (levelVisual != null)
            Destroy(levelVisual);
        ZoneStatus = "Outside event zone";
    }
    void Receive(ServerMessage m)
    {
        if (m.Kind == EventKind.Welcome)
        {
            Clear();
            CurrentMap = (int)m.Value;
            CurrentLevel = WorldCatalog != null ? WorldCatalog.Get(CurrentMap) : null;
            if (CurrentLevel != null)
            {
                MapCenter = CurrentLevel.Center;
                MapRadius = CurrentLevel.Radius;
                levelVisual = Instantiate(CurrentLevel.Visual, transform);
                UIEvents.EvMinimapImageChanged(CurrentLevel.Minimap, CurrentLevel.MinimapPosition,
                                               CurrentLevel.MinimapRotation, CurrentLevel.MinimapScale);
            }
            return;
        }
        if (m.Kind == EventKind.Spawn)
        {
            Spawn(m);
            return;
        }
        if (m.Kind == EventKind.Despawn)
        {
            if (entities.TryGetValue(m.Entity, out var gone))
            {
                entities.Remove(m.Entity);
                Destroy(gone.gameObject);
            }
            return;
        }
        if (m.Kind == EventKind.Inventory)
            inventory[m.Definition] = (int)m.Value;
        if (m.Kind == EventKind.Zone && m.Entity == Transport.LocalEntityId)
            ZoneStatus = $"Zone {m.Target}: {((m.Flags&1)!=0?"Entered":"Exited")}";
        if (entities.TryGetValue(m.Entity, out var actor))
            actor.Receive(m);
    }
    void Spawn(ServerMessage m)
    {
        if (entities.ContainsKey(m.Entity))
            return;
        var kind = (EntityKind)m.Code;
        if (kind == EntityKind.Boundary)
        {
            MapRadius = m.Radius;
            MapCenter = m.Position;
            if (!DebugGeometry)
                return;
            var boundary = new GameObject($"Boundary_{m.Entity}");
            boundary.transform.SetParent(transform, false);
            DrawRing(boundary, m.Radius, new Color(1, .5f, .1f));
            boundary.AddComponent<ServerTransceiver>().Bind(this, m);
            return;
        }
        var root = new GameObject($"{kind}_{m.Entity}");
        root.transform.SetParent(transform, false);
        root.transform.position = m.Position;
        var actor = root.AddComponent<ServerTransceiver>();
        GameObject visual = null;
        ActionVisualProfile profile = null;
        if (kind == EntityKind.Player)
        {
            visual = PlayerVisual;
            profile = PlayerProfile;
        }
        else if (kind == EntityKind.Monster)
        {
            int i = m.Definition;
            if (MonsterVisuals != null && i >= 0 && i < MonsterVisuals.Length)
                visual = MonsterVisuals[i];
            if (MonsterProfiles != null && i >= 0 && i < MonsterProfiles.Length)
                profile = MonsterProfiles[i];
        }
        else if (kind == EntityKind.Resource)
            visual = Catalog != null && Catalog.Visual(m.Definition, false) != null
                         ? Catalog.Visual(m.Definition, false)
                         : ResourceVisual;
        else if (kind == EntityKind.Wall)
            visual = DebugGeometry ? WallVisual : null;
        else if (kind == EntityKind.Placement)
            visual = Catalog != null ? Catalog.Visual(m.Definition, true) : null;
        if (visual != null)
        {
            var child = Instantiate(visual, root.transform);
            foreach (var animator in child.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.fireEvents = false;
            }
            child.transform.localPosition = profile != null ? profile.VisualOffset : Vector3.zero;
            child.transform.localRotation =
                Quaternion.Euler(profile != null ? profile.VisualEuler : Vector3.zero);
            if (profile != null)
                child.transform.localScale *= profile.VisualScale;
            if (kind == EntityKind.Wall)
            {
                child.transform.localPosition = Vector3.up * m.Velocity.y;
                child.transform.localScale = Vector3.Scale(child.transform.localScale, m.Velocity * 2);
            }
            else if (kind == EntityKind.Resource &&
                     (Catalog == null || Catalog.Visual(m.Definition, false) == null))
            {
                child.transform.localPosition = Vector3.up;
                child.transform.localScale =
                    Vector3.Scale(child.transform.localScale, new Vector3(m.Radius * 2, 2, m.Radius * 2));
            }
        }
        if (kind == EntityKind.Wall)
        {
            root.layer = 9;
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(m.Velocity.x * 2, m.Velocity.y * 2, m.Velocity.z * 2);
            box.center = Vector3.up * m.Velocity.y;
            if (visual == null && DebugGeometry)
                CreateShape(root, PrimitiveType.Cube, box.center, box.size, new Color(.35f, .4f, .48f));
        }
        else if (kind == EntityKind.Zone)
        {
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = m.Radius;
            sphere.isTrigger = true;
            if (DebugGeometry)
                DrawRing(root, m.Radius, Color.cyan);
        }
        else if (kind != EntityKind.Placement)
        {
            if (kind == EntityKind.Resource)
            {
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(m.Radius * 2, 2, m.Radius * 2);
                box.center = Vector3.up;
            }
            else
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = m.Radius;
                capsule.height = 2;
                capsule.center = Vector3.up;
                capsule.isTrigger = true;
            }
            root.layer = kind == EntityKind.Resource ? 9 : 10;
            if (visual == null)
                CreateShape(root, PrimitiveType.Capsule, Vector3.up,
                            new Vector3(m.Radius * 2, 1, m.Radius * 2),
                            kind == EntityKind.Player ? Color.blue : Color.red);
        }
        if (kind == EntityKind.Player || kind == EntityKind.Monster || kind == EntityKind.Resource)
        {
            var presentation = root.AddComponent<EntityPresentation>();
            presentation.Actor = actor;
            presentation.Profile = profile;
            presentation.Animator = root.GetComponentInChildren<Animator>();
            if (presentation.Animator != null && profile != null)
                presentation.Animator.runtimeAnimatorController = profile.Controller;
            if (kind == EntityKind.Player && m.Entity == Transport.LocalEntityId)
            {
                var motor = root.AddComponent<MovementMotor>();
                motor.Actor = actor;
                motor.Radius = m.Radius;
                motor.MoveSpeed = m.Duration > 0 ? m.Duration : 5;
                motor.BlockingMask = 1 << 9;
                var control = root.AddComponent<PlayerActionController>();
                control.Actor = actor;
                control.Motor = motor;
                root.AddComponent<GameplayTargetIndicator>().Controller = control;
            }
        }
        if (kind == EntityKind.Resource)
            root.AddComponent<ResourceRefillController>().Actor = actor;
        actor.Bind(this, m);
        if (kind == EntityKind.Placement)
            root.transform.rotation = Quaternion.Euler(0, m.Velocity.y, 0);
    }
    static void CreateShape(GameObject root, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var child = GameObject.CreatePrimitive(type);
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = position;
        child.transform.localScale = scale;
        Destroy(child.GetComponent<Collider>());
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        child.GetComponent<Renderer>().sharedMaterial = material;
        child.AddComponent<RuntimeMaterialOwner>().Value = material;
    }
    static void DrawRing(GameObject root, float radius, Color color)
    {
        var line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 96;
        line.widthMultiplier = .08f;
        line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        root.AddComponent<RuntimeMaterialOwner>().Value = line.sharedMaterial;
        line.startColor = line.endColor = color;
        for (int i = 0; i < 96; i++)
        {
            float angle = i * Mathf.PI * 2 / 96;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .05f, Mathf.Sin(angle) * radius));
        }
    }
}
}
