using System;

using System.Collections;

using System.Collections.Generic;

using EverWind.Gameplay;

using UnityEngine;

using UnityEngine.SceneManagement;

public class WorldLoader : SingletonBase<WorldLoader>
{
    private GameObject worldRoot;

    private Collider[] groundColliders = Array.Empty<Collider>();

    private readonly Dictionary<GameObject, List<Material>> ownedMaterials = new Dictionary<GameObject, List<Material>>();
    private readonly Dictionary<int, List<ServerMessage>> waiting = new Dictionary<int, List<ServerMessage>>();

    private readonly Dictionary<int, MonsterServerTransmitter> inactiveMonsters = new Dictionary<int, MonsterServerTransmitter>();

    public ContainerInMap Container;

    public ServerTransmitterManager Registry;

    public Canvas LoadingCanvas;

    public TMPro.TMP_Text LoadingLabel;

    public GameObject InstancedPlayer => Registry != null && Registry.LocalActor != null ? Registry.LocalActor.gameObject : null;

    public override bool IsPersistent => true;

    private NetworkClient Transport => Container.Transport;

    private GameplayCatalog Catalog => Container.Catalog;

    private bool DebugGeometry => Registry != null && Registry.DebugGeometry;

    private Transform SpawnParent => worldRoot.transform;

    protected override void Awake()
    {
        Priority = -60;

        base.Awake();

        if (LoadingCanvas != null)
        {
            DontDestroyOnLoad(LoadingCanvas.gameObject);
        }
    }

    public void SetLoading(bool loading)
    {
        if (LoadingCanvas != null)
        {
            LoadingCanvas.gameObject.SetActive(loading);
        }

        if (LoadingLabel != null)
        {
            LoadingLabel.text = "로딩중…";
        }
    }

    private static IEnumerator EnsureGameScene()
    {
        if (SceneManager.GetActiveScene().name == "InGame")
        {
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync("InGame");

        yield return operation;
    }

    public void BeginWorld(ContainerInMap container, int version)
    {
        Container = container;

        StartCoroutine(LoadWorld(version));
    }

    private IEnumerator LoadWorld(int version)
    {
        yield return Container.PrepareAssets(version);

        if (!Container.IsCurrent(version))
        {
            yield break;
        }

        if (!Container.AssetsReady)
        {
            Transport.Fail(Container.LoadError ?? "맵 에셋 준비에 실패했습니다.");

            yield break;
        }

        yield return EnsureGameScene();

        if (!Container.IsCurrent(version))
        {
            yield break;
        }

        Registry = UnityEngine.Object.FindFirstObjectByType<ServerTransmitterManager>();

        if (Registry == null)
        {
            Transport.Fail("월드 개체 등록기가 없습니다.");

            yield break;
        }
        // 씬 초기화가 완료된 뒤 수신 데이터를 재생한다.
        yield return null;

        if (!Container.IsCurrent(version))
        {
            yield break;
        }
        // 씬 전환 중 새로 등장한 종류까지 준비한 뒤 생성한다.
        yield return Container.PrepareAssets(version);

        if (!Container.IsCurrent(version))
        {
            yield break;
        }

        if (!Container.AssetsReady)
        {
            Transport.Fail(Container.LoadError ?? "맵 에셋 준비에 실패했습니다.");

            yield break;
        }

        worldRoot = new GameObject("World_" + Container.MapId);

        SceneManager.MoveGameObjectToScene(worldRoot, SceneManager.GetActiveScene());

        GameObject mapPrefab = Container.MapPrefab;

        if (mapPrefab == null)
        {
            Transport.Fail("맵 외형을 찾을 수 없습니다.");

            yield break;
        }

        GameObject map = Instantiate(mapPrefab, worldRoot.transform);

        groundColliders = map.GetComponentsInChildren<Collider>(true);

        Physics.SyncTransforms();

        Container.PublishBuffered(version);

        if (!Container.IsCurrent(version))
        {
            yield break;
        }

        if (Registry.LocalActor == null || waiting.Count != 0)
        {
            Transport.Fail("월드 초기 개체 준비가 완료되지 않았습니다.");

            yield break;
        }

        Transport.CompleteWorld();

        SetLoading(false);
    }

    public void ReceiveSpawn(ServerMessage message)
    {
        if (Registry.TryGet(message.Entity, out _) || waiting.ContainsKey(message.Entity))
        {
            return;
        }

        if (Container.HasEntityAssets(message))
        {
            Spawn(message);

            return;
        }

        waiting[message.Entity] = new List<ServerMessage>();

        StartCoroutine(SpawnWhenReady(message, Container.Generation));
    }

    private IEnumerator SpawnWhenReady(ServerMessage message, int version)
    {
        yield return Container.PrepareEntity(message, version);

        if (!Container.IsCurrent(version) || !Container.Spawns.TryGetValue(message.Entity, out ServerMessage current) || !ReferenceEquals(current, message) || !waiting.TryGetValue(message.Entity, out List<ServerMessage> messages))
        {
            yield break;
        }

        if (Container.LoadError != null)
        {
            Transport.Fail(Container.LoadError);

            yield break;
        }

        Spawn(message);

        waiting.Remove(message.Entity);

        if (Registry.TryGet(message.Entity, out ServerTransmitter actor))
        {
            foreach (ServerMessage pending in messages)
            {
                actor.Receive(pending);
            }
        }
    }

    public void BufferEntityEvent(ServerMessage message)
    {
        if (message.Kind != EventKind.Spawn && waiting.TryGetValue(message.Entity, out List<ServerMessage> messages))
        {
            messages.Add(message);
        }
    }

    public void Despawn(int id)
    {
        waiting.Remove(id);

        if (Registry.TryGet(id, out ServerTransmitter actor))
        {
            Registry.Unregister(actor);

            actor.gameObject.SetActive(false);

            if (actor is MonsterServerTransmitter monster)
            {
                inactiveMonsters[id] = monster;

                return;
            }

            ReleaseMaterials(actor.gameObject);

            Destroy(actor.gameObject);
        }
    }

    public void ClearWorld()
    {
        StopAllCoroutines();

        waiting.Clear();
        inactiveMonsters.Clear();

        groundColliders = Array.Empty<Collider>();

        Registry?.Clear();

        if (worldRoot != null)
        {
            worldRoot.SetActive(false);

            Destroy(worldRoot);

            worldRoot = null;
        }

        ReleaseMaterials();
    }

    public bool TryGetGroundHeight(Vector3 position, float stepUp, float stepDown, out float height)
    {
        height = position.y;

        if (stepUp < 0 || stepDown < 0)
        {
            return false;
        }

        Ray ray = new Ray(position + Vector3.up * stepUp, Vector3.down);

        float distance = stepUp + stepDown;
        float nearest = float.PositiveInfinity;

        int groundLayer = LayerMask.NameToLayer("Ground");
        // 현재 맵의 지형만 검사하므로 다른 개체와 트리거는 발판이 되지 않는다.
        foreach (Collider collider in groundColliders)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger || collider.gameObject.layer != groundLayer || collider.GetComponentInParent<ServerTransmitter>() != null)
            {
                continue;
            }

            if (collider.Raycast(ray, out RaycastHit hit, distance) && hit.normal.y >= .5f && hit.distance < nearest)
            {
                nearest = hit.distance;

                height = hit.point.y;
            }
        }

        return nearest < float.PositiveInfinity;
    }

    private ServerTransmitter AttachTransmitter(GameObject root, EntityKind kind, int entity)
    {
        if (kind == EntityKind.Player)
        {
            return entity == Transport.LocalEntityId ? root.AddComponent<PlayerServerTransmitter>() : root.AddComponent<OtherPlayerServerTransmitter>();
        }

        if (kind == EntityKind.Monster)
        {
            return root.AddComponent<MonsterServerTransmitter>();
        }

        if (kind == EntityKind.Resource)
        {
            return root.AddComponent<ResourceServerTransmitter>();
        }

        return root.AddComponent<ServerTransmitter>();
    }

    private bool TryReuseMonster(ServerMessage spawn)
    {
        if (!inactiveMonsters.TryGetValue(spawn.Entity, out MonsterServerTransmitter monster))
        {
            return false;
        }

        inactiveMonsters.Remove(spawn.Entity);

        if (monster == null)
        {
            return false;
        }

        // 같은 ID라도 몬스터 종류가 바뀌면 기존 외형을 재사용하지 않는다.
        if (monster.Definition != spawn.Definition)
        {
            ReleaseMaterials(monster.gameObject);

            Destroy(monster.gameObject);

            return false;
        }

        // 비활성 상태에서 서버 스폰 정보와 표현 상태를 초기화한 뒤 활성화한다.
        monster.Bind(Registry, spawn);

        foreach (Collider collider in monster.GetComponents<Collider>())
        {
            collider.enabled = true;
        }

        monster.gameObject.SetActive(true);

        return true;
    }

    private void Spawn(ServerMessage m)
    {
        if (Registry.TryGet(m.Entity, out _))
        {
            return;
        }

        var kind = (EntityKind)m.Code;

        if (kind == EntityKind.Boundary)
        {
            if (!DebugGeometry)
            {
                return;
            }

            var boundary = new GameObject($"Boundary_{m.Entity}");

            boundary.transform.SetParent(SpawnParent, false);

            DrawRing(boundary, m.Radius, new Color(1, .5f, .1f));

            boundary.AddComponent<ServerTransmitter>().Bind(Registry, m);

            return;
        }

        if (kind == EntityKind.Monster && TryReuseMonster(m))
        {
            return;
        }

        var root = new GameObject($"{kind}_{m.Entity}");

        root.transform.SetParent(SpawnParent, false);

        root.transform.position = m.Position;

        var actor = AttachTransmitter(root, kind, m.Entity);

        GameObject visual = null;

        ActionVisualProfile profile = null;

        if (kind == EntityKind.Player)
        {
            visual = Container.GetAsset<GameObject>(Container.PlayerVisual);
            profile = Container.GetAsset<ActionVisualProfile>(Container.PlayerProfile);
        }
        else if (kind == EntityKind.Monster)
        {
            int i = m.Definition;

            if (i >= 0 && i < Container.MonsterVisuals.Length)
            {
                visual = Container.GetAsset<GameObject>(Container.MonsterVisuals[i]);
            }

            if (i >= 0 && i < Container.MonsterProfiles.Length)
            {
                profile = Container.GetAsset<ActionVisualProfile>(Container.MonsterProfiles[i]);
            }
        }
        else if (kind == EntityKind.Resource)
            visual = Catalog != null && Catalog.Visual(m.Definition, false) != null
                         ? Catalog.Visual(m.Definition, false)
                         : null;
        else if (kind == EntityKind.Wall)
            visual = null;
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
            {
                child.transform.localScale *= profile.VisualScale;
            }

            if (kind == EntityKind.Wall)
            {
                child.transform.localPosition = Vector3.up * m.Velocity.y;

                child.transform.localScale = Vector3.Scale(child.transform.localScale, m.Velocity * 2);
            }
            else if (kind == EntityKind.Resource && (Catalog == null || Catalog.Visual(m.Definition, false) == null))
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
            {
                CreateShape(root, PrimitiveType.Cube, box.center, box.size, new Color(.35f, .4f, .48f));
            }
        }
        else if (kind == EntityKind.Zone)
        {
            var sphere = root.AddComponent<SphereCollider>();

            sphere.radius = m.Radius;

            sphere.isTrigger = true;

            if (DebugGeometry)
            {
                DrawRing(root, m.Radius, Color.cyan);
            }
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
                CreateShape(root, PrimitiveType.Capsule, Vector3.up, new Vector3(m.Radius * 2, 1, m.Radius * 2), kind == EntityKind.Player ? Color.blue : Color.red);
        }

        if (kind == EntityKind.Player || kind == EntityKind.Monster || kind == EntityKind.Resource)
        {
            if (actor is AliveServerTransmitter alive)
            {
                alive.ConfigureVisual(profile, root.GetComponentInChildren<Animator>());
            }

            if (kind == EntityKind.Player && m.Entity == Transport.LocalEntityId)
            {
                var motor = root.AddComponent<MovementMotor>();

                motor.Actor = (PlayerServerTransmitter)actor;

                motor.Radius = m.Radius;

                motor.MoveSpeed = m.Duration > 0 ? m.Duration : 5;

                motor.BlockingMask = 1 << 9;

                var control = root.AddComponent<PlayerActionController>();

                control.Actor = (PlayerServerTransmitter)actor;

                control.Motor = motor;
            }
        }

        actor.Bind(Registry, m);

        if (kind == EntityKind.Placement)
        {
            root.transform.rotation = Quaternion.Euler(0, m.Velocity.y, 0);
        }
    }

    private void CreateShape(GameObject root, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var child = GameObject.CreatePrimitive(type);

        child.transform.SetParent(root.transform, false);

        child.transform.localPosition = position;

        child.transform.localScale = scale;

        Destroy(child.GetComponent<Collider>());

        var material = new Material(Shader.Find("Standard"));

        material.color = color;

        child.GetComponent<Renderer>().sharedMaterial = material;

        TrackMaterial(child, material);
    }

    private void DrawRing(GameObject root, float radius, Color color)
    {
        var line = root.AddComponent<LineRenderer>();

        line.useWorldSpace = false;

        line.loop = true;

        line.positionCount = 96;

        line.widthMultiplier = .08f;

        line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        TrackMaterial(root, line.sharedMaterial);

        line.startColor = line.endColor = color;

        for (int i = 0; i < 96; i++)
        {
            float angle = i * Mathf.PI * 2 / 96;

            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .05f, Mathf.Sin(angle) * radius));
        }
    }

    private void TrackMaterial(GameObject owner, Material material)
    {
        if (!ownedMaterials.TryGetValue(owner, out List<Material> materials))
        {
            materials = new List<Material>();

            ownedMaterials.Add(owner, materials);
        }

        materials.Add(material);
    }

    private void ReleaseMaterials(GameObject owner = null)
    {
        List<GameObject> released = new List<GameObject>();

        foreach (KeyValuePair<GameObject, List<Material>> entry in ownedMaterials)
        {
            if (owner != null && entry.Key != null && entry.Key != owner && !entry.Key.transform.IsChildOf(owner.transform))
            {
                continue;
            }

            foreach (Material material in entry.Value)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            released.Add(entry.Key);
        }

        foreach (GameObject key in released)
        {
            ownedMaterials.Remove(key);
        }
    }

    private void LateUpdate()
    {
        // 외부에서 개별 도형을 제거한 경우에도 소유 머티리얼을 반환한다.
        List<GameObject> destroyed = null;

        foreach (KeyValuePair<GameObject, List<Material>> entry in ownedMaterials)
        {
            if (entry.Key != null)
            {
                continue;
            }

            if (destroyed == null)
            {
                destroyed = new List<GameObject>();
            }

            destroyed.Add(entry.Key);
        }

        if (destroyed == null)
        {
            return;
        }

        foreach (GameObject key in destroyed)
        {
            foreach (Material material in ownedMaterials[key])
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            ownedMaterials.Remove(key);
        }
    }

    protected override void OnDestroy()
    {
        ClearWorld();

        base.OnDestroy();
    }
}
