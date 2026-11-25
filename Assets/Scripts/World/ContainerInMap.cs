using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EverWind.Gameplay
{
[DefaultExecutionOrder(-300)]
public sealed class ContainerInMap : MonoBehaviour
{
    [Serializable]
    public struct MapEntry
    {
        public int Id;
        public AssetReferenceGameObject Prefab;
    }

    private readonly Dictionary<string, AsyncOperationHandle> commonAssets = new Dictionary<string, AsyncOperationHandle>();
    private readonly Dictionary<string, AsyncOperationHandle> mapAssets = new Dictionary<string, AsyncOperationHandle>();
    private readonly List<ServerMessage> buffered = new List<ServerMessage>();
    private readonly Dictionary<int, ServerMessage> spawns = new Dictionary<int, ServerMessage>();
    private readonly List<Dictionary<string, AsyncOperationHandle>> retiredAssets = new List<Dictionary<string, AsyncOperationHandle>>();
    private int generation;

    public static ContainerInMap Current { get; private set; }
    public NetworkClient Transport;
    public AssetReferenceT<GameplayCatalog> CommonCatalog;
    public AssetReferenceGameObject PlayerVisual, TargetIndicatorVisual;
    public AssetReferenceT<ActionVisualProfile> PlayerProfile;
    public AssetReferenceGameObject[] MonsterVisuals;
    public AssetReferenceT<ActionVisualProfile>[] MonsterProfiles;
    public MapEntry[] Maps;
    public WorldLoader Loader;
    public GameplayCatalog Catalog { get; private set; }
    public GameObject MapPrefab { get; private set; }
    public Vector3 MapCenter { get; private set; }
    public float MapRadius { get; private set; }
    public bool HasMapBoundary { get; private set; }
    public int MapId { get; private set; }
    public uint Epoch { get; private set; }
    public int Generation => generation;
    public bool SnapshotComplete { get; private set; }
    public bool Rendered { get; private set; }
    public bool AssetsReady { get; private set; }
    public string LoadError { get; private set; }
    public IReadOnlyDictionary<int, ServerMessage> Spawns => spawns;
    public int CommonHandleCount => commonAssets.Count;
    public int MapHandleCount => mapAssets.Count;

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            if (Loader != null && Loader.LoadingCanvas != null && Loader.LoadingCanvas != Current.Loader.LoadingCanvas)
            {
                Destroy(Loader.LoadingCanvas.gameObject);
            }

            Destroy(gameObject);
            return;
        }

        Current = this;
        DontDestroyOnLoad(transform.root.gameObject);
        Transport = NetworkClient.Current;
    }

    public void SetLoading(bool value)
    {
        Loader?.SetLoading(value);
    }

    public void Receive(ServerMessage message)
    {
        if (message.Kind == EventKind.Welcome)
        {
            ClearMap();
            Epoch = message.Epoch;
            MapId = (int)message.Value;
            SetLoading(true);
        }

        if (message.Epoch != Epoch)
        {
            return;
        }

        if (message.Kind == EventKind.Spawn && (EntityKind)message.Code == EntityKind.Boundary)
        {
            if (float.IsNaN(message.Radius) || float.IsInfinity(message.Radius) || message.Radius <= 0 || float.IsNaN(message.Position.x) || float.IsInfinity(message.Position.x) || float.IsNaN(message.Position.z) || float.IsInfinity(message.Position.z))
            {
                Transport.Fail("서버 맵 경계 정보가 올바르지 않습니다.");
                return;
            }

            MapCenter = message.Position;
            MapRadius = message.Radius;
            HasMapBoundary = true;
        }

        if (message.Kind == EventKind.Spawn)
        {
            spawns[message.Entity] = message;
        }
        else if (message.Kind == EventKind.Despawn)
        {
            spawns.Remove(message.Entity);
        }

        if (Rendered)
        {
            PacketMethod.ApplyWorldPacket(message);
            return;
        }

        if (buffered.Count >= 65536)
        {
            Transport.Fail("맵 준비 중 수신 데이터 한도를 초과했습니다.");
            return;
        }

        buffered.Add(message);

        if (message.Kind == EventKind.SnapshotEnd && !SnapshotComplete)
        {
            if (!HasMapBoundary)
            {
                Transport.Fail("서버 맵 경계 정보를 받지 못했습니다.");
                return;
            }

            SnapshotComplete = true;
            Loader.BeginWorld(this, generation);
        }
    }

    public IEnumerator PrepareAssets(int version)
    {
        AssetsReady = false;
        LoadError = null;

        yield return Load(CommonCatalog, true, version);

        if (!IsCurrent(version))
        {
            yield break;
        }

        Catalog = GetAsset<GameplayCatalog>(CommonCatalog);

        if (Catalog == null)
        {
            LoadError = "공통 카탈로그를 찾을 수 없습니다.";
            yield break;
        }

        yield return Load(PlayerVisual, true, version);
        yield return Load(PlayerProfile, true, version);

        foreach (ItemVisualEntry entry in Catalog.Visuals)
        {
            yield return Load(entry.PlacementReference, true, version);
        }

        AssetReferenceGameObject mapReference = null;

        foreach (MapEntry entry in Maps)
        {
            if (entry.Id == MapId)
            {
                mapReference = entry.Prefab;
                break;
            }
        }

        if (mapReference == null || !mapReference.RuntimeKeyIsValid())
        {
            LoadError = "맵 ID에 대응하는 프리팹이 없습니다: " + MapId;
            yield break;
        }

        yield return Load(mapReference, false, version);

        if (!IsCurrent(version))
        {
            yield break;
        }

        MapPrefab = GetAsset<GameObject>(mapReference);

        if (MapPrefab == null)
        {
            LoadError = "맵 프리팹 로딩에 실패했습니다.";
            yield break;
        }

        // 에셋 로딩 중 추가된 종류도 최종 생성 전에 준비한다.
        while (IsCurrent(version))
        {
            List<ServerMessage> pending = new List<ServerMessage>(spawns.Values);

            foreach (ServerMessage spawn in pending)
            {
                yield return PrepareEntity(spawn, version);
            }

            bool complete = true;

            foreach (ServerMessage spawn in spawns.Values)
            {
                if (!HasEntityAssets(spawn))
                {
                    complete = false;
                    break;
                }
            }

            if (LoadError != null || complete)
            {
                break;
            }
        }

        AssetsReady = IsCurrent(version) && LoadError == null;
    }

    public IEnumerator PrepareEntity(ServerMessage spawn, int version)
    {
        EntityKind kind = (EntityKind)spawn.Code;

        if (kind == EntityKind.Monster)
        {
            if (spawn.Definition < 0 || spawn.Definition >= MonsterVisuals.Length || spawn.Definition >= MonsterProfiles.Length)
            {
                LoadError = "몬스터 종류에 대응하는 외형이 없습니다: " + spawn.Definition;
                yield break;
            }

            yield return Load(MonsterVisuals[spawn.Definition], false, version);
            yield return Load(MonsterProfiles[spawn.Definition], false, version);
        }
        else if (kind == EntityKind.Resource || kind == EntityKind.Placement)
        {
            ItemVisualEntry entry = FindVisual(spawn.Definition);

            if (entry == null)
            {
                LoadError = "채집물 또는 설치물 외형이 없습니다: " + spawn.Definition;
                yield break;
            }

            AssetReference reference = kind == EntityKind.Resource ? entry.ResourceReference : entry.PlacementReference;

            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                LoadError = "필수 엔티티 에셋 참조가 없습니다: " + spawn.Definition;
                yield break;
            }

            yield return Load(reference, false, version);
        }
    }

    public bool HasEntityAssets(ServerMessage spawn)
    {
        EntityKind kind = (EntityKind)spawn.Code;

        if (kind == EntityKind.Monster)
        {
            return spawn.Definition >= 0 && spawn.Definition < MonsterVisuals.Length && spawn.Definition < MonsterProfiles.Length && GetAsset<GameObject>(MonsterVisuals[spawn.Definition]) != null && GetAsset<ActionVisualProfile>(MonsterProfiles[spawn.Definition]) != null;
        }

        if (kind == EntityKind.Resource || kind == EntityKind.Placement)
        {
            ItemVisualEntry entry = FindVisual(spawn.Definition);

            return entry != null && GetAsset<GameObject>(kind == EntityKind.Resource ? entry.ResourceReference : entry.PlacementReference) != null;
        }

        return true;
    }

    public ItemVisualEntry FindVisual(int id)
    {
        if (Catalog != null)
        {
            foreach (ItemVisualEntry entry in Catalog.Visuals)
            {
                if (entry.Item == id)
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private IEnumerator Load(AssetReference reference, bool common, int version)
    {
        if (!IsCurrent(version) || LoadError != null || reference == null || !reference.RuntimeKeyIsValid())
        {
            yield break;
        }

        string key = reference.AssetGUID;

        if (commonAssets.TryGetValue(key, out AsyncOperationHandle existing) || mapAssets.TryGetValue(key, out existing))
        {
            yield return existing;

            if (IsCurrent(version) && existing.IsValid() && existing.Status != AsyncOperationStatus.Succeeded)
            {
                LoadError = "에셋 로딩 실패: " + key;
            }

            yield break;
        }

        AsyncOperationHandle<UnityEngine.Object> handle = Addressables.LoadAssetAsync<UnityEngine.Object>(reference.RuntimeKey);

        Dictionary<string, AsyncOperationHandle> assets = common ? commonAssets : mapAssets;

        assets.Add(key, handle);
        yield return handle;

        if (IsCurrent(version) && handle.IsValid() && handle.Status != AsyncOperationStatus.Succeeded)
        {
            LoadError = "에셋 로딩 실패: " + key;
        }
    }

    public T GetAsset<T>(AssetReference reference)
        where T : UnityEngine.Object
    {
        if (reference == null || !reference.RuntimeKeyIsValid())
        {
            return null;
        }

        if ((commonAssets.TryGetValue(reference.AssetGUID, out AsyncOperationHandle handle) || mapAssets.TryGetValue(reference.AssetGUID, out handle)) && handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
        {
            return handle.Result as T;
        }

        return null;
    }

    public bool IsCurrent(int version)
    {
        return generation == version && Current == this;
    }

    public void PublishBuffered(int version)
    {
        if (!IsCurrent(version))
        {
            return;
        }

        // 로딩 중 들어온 변경도 수신 순서대로 적용한다.
        for (int i = 0; i < buffered.Count; ++i)
        {
            PacketMethod.ApplyWorldPacket(buffered[i]);

            if (!IsCurrent(version))
            {
                return;
            }
        }

        buffered.Clear();
        Rendered = true;
    }

    private static void Release(Dictionary<string, AsyncOperationHandle> assets)
    {
        foreach (AsyncOperationHandle handle in assets.Values)
        {
            if (handle.IsValid())
            {
                if (handle.IsDone)
                {
                    Addressables.Release(handle);
                }
                else
                {
                    handle.Completed += completed => Addressables.Release(completed);
                }
            }
        }

        assets.Clear();
    }

    private void Retire(Dictionary<string, AsyncOperationHandle> assets)
    {
        Dictionary<string, AsyncOperationHandle> retired = new Dictionary<string, AsyncOperationHandle>(assets);

        assets.Clear();
        retiredAssets.Add(retired);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ReleaseAfterDestroy(retired));
        }
        else
        {
            Release(retired);
            retiredAssets.Remove(retired);
        }
    }

    private IEnumerator ReleaseAfterDestroy(Dictionary<string, AsyncOperationHandle> retired)
    {
        // Destroy 예약된 월드의 수명이 끝난 뒤 에셋 참조를 반환한다.
        yield return null;

        Release(retired);
        retiredAssets.Remove(retired);
    }

    public void ClearMap()
    {
        ++generation;
        Loader?.ClearWorld();
        buffered.Clear();
        spawns.Clear();
        MapPrefab = null;
        MapCenter = Vector3.zero;
        MapRadius = 0;
        HasMapBoundary = false;
        Rendered = false;
        SnapshotComplete = false;
        AssetsReady = false;
        LoadError = null;
        Retire(mapAssets);
    }

    public void ClearAll()
    {
        ClearMap();
        Catalog = null;
        Retire(commonAssets);
        SetLoading(false);
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            ClearAll();
            StopAllCoroutines();

            foreach (Dictionary<string, AsyncOperationHandle> retired in retiredAssets)
            {
                Release(retired);
            }

            retiredAssets.Clear();
            Current = null;
        }
    }
}
}
