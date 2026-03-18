using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class WorldLoader : SingletonBase<WorldLoader>
{
    private GameObject _currentMapInstance;

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _otherPlayerPrefab;
    public GameObject InstancedPlayer { get; private set; }
    public override bool IsPersistent => true;

    protected override void Awake()
    {
        Priority = -60;
        base.Awake();
    }

    public void InitializeWorld(int mapId, Vector3 spawnPos, Queue<int> otherPlayers, Queue<DataCenter.EnemyInfo> enemies)
    {
        ClearWorld();

        SpawnMap(mapId);

        SpawnLocalPlayer(spawnPos);

        SpawnOtherPlayers(otherPlayers);

        SpawnEnemies(enemies);
    }

    private void SpawnMap(int mapId)
    {
        // Use DataCenter MapTable to resolve the map prefab.
        if (DataCenter.Instance.MapTable.TryGetValue(mapId, out GameObject mapPrefab))
        {
            if (mapPrefab == null)
            {
                Debug.LogError($"[WorldLoader] MapId {mapId} prefab is null in DataCenter.");
                return;
            }

            _currentMapInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            _currentMapInstance.name = $"Map_{mapId}";
        }
        else
        {
            Debug.LogError($"[WorldLoader] MapId {mapId} not found in DataCenter MapTable.");
        }
    }

    private void SpawnLocalPlayer(Vector3 spawnPos)
    {
        if (_playerPrefab != null)
        {
            InstancedPlayer = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            InstancedPlayer.GetComponent<Player>().Init();
            SetupCamera(InstancedPlayer.transform);
        }
    }

    private void SpawnOtherPlayers(Queue<int> otherPlayers)
    {
        if (otherPlayers == null) return;
        var otherPlayerManager = SingletonManager.Instance.GetSingleton<OtherPlayerManager>();

        while (otherPlayers.Count > 0)
        {
            var otherPlayerId = otherPlayers.Dequeue();
            GameObject go = Instantiate(_otherPlayerPrefab);
            OtherPlayer otherPlayer = go.GetComponent<OtherPlayer>();
            otherPlayerManager.RegisterPlayer(otherPlayerId, otherPlayer);
        }
    }

    public void SpawnOtherPlayers(int otherPlayerId)
    {
        var otherPlayerManager = SingletonManager.Instance.GetSingleton<OtherPlayerManager>();
        GameObject go = Instantiate(_otherPlayerPrefab);
        OtherPlayer otherPlayer = go.GetComponent<OtherPlayer>();
        otherPlayerManager.RegisterPlayer(otherPlayerId, otherPlayer);
    }

    private void SetupCamera(Transform target)
    {
        var camObj = Camera.main;
        if (camObj == null) return;

        CameraMoving cam = camObj.GetComponent<CameraMoving>();
        if (cam != null)
        {
            cam.Target = target;
            var player = target.GetComponent<Player>();
            if (player != null) player.CameraTransform = cam.transform;
        }
    }

    public void ClearWorld()
    {
        if (_currentMapInstance != null) Destroy(_currentMapInstance);
        if (InstancedPlayer != null) Destroy(InstancedPlayer);
    }

    private void SpawnEnemies(Queue<DataCenter.EnemyInfo> enemies)
    {
        // Use DataCenter MonsterTable to resolve enemy prefabs.
        var monsterTable = SingletonManager.Instance.GetSingleton<DataCenter>().MonsterTable;
        var enemySpawner = SingletonManager.Instance.GetSingleton<EnemySpawner>();

        int groundLayerMask = LayerMask.GetMask("Ground");

        while (enemies.Count > 0)
        {
            var enemyInfo = enemies.Dequeue();
            if (!monsterTable.ContainsKey(enemyInfo.EnemyId)) continue;

            var enemyPrefab = monsterTable[enemyInfo.EnemyId];
            Vector3 spawnPos = enemyInfo.Position;

            Vector3 rayStart = spawnPos + Vector3.up;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000.0f, groundLayerMask))
            {
                spawnPos.y = hit.point.y;
            }

            var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity).GetComponent<Enemy>();
            go.InstanceNum = enemyInfo.InstanceId;
            enemySpawner.RegisterEnemy(go.InstanceNum, go);
        }
    }
}
