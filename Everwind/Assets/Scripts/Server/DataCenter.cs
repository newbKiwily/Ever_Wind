using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class DataCenter : SingletonBase<DataCenter>
{
    public override bool IsPersistent => true;

    protected override void Awake()
    {
        Priority = -80;
        base.Awake();
    }

    public struct LoginData
    {
        public Vector3 Position;
        public int MapId;
    }

    public struct InventoryData
    {
        public string Key;
        public int Amount;
        public int SlotIndex;
        public int IsEquppiedItem;
    }

    public struct EnemyInfo
    {
        public int InstanceId;
        public int EnemyId;
        public Vector3 Position;
    }


    public LoginData loginData = new LoginData();

    public SerializedDictionary<int, MapData> MapTable = new SerializedDictionary<int, MapData>();
    public SerializedDictionary<int, GameObject> MonsterTable = new SerializedDictionary<int, GameObject>();
    public List<InventoryData> LoadItems = new List<InventoryData>();
    public PlayerStatManager.Stat LoadStat = new PlayerStatManager.Stat();
    public Queue<string> LoadEquipItems = new Queue<string>();
    public Queue<int> OtherPlayers = new Queue<int>();
    public Queue<EnemyInfo> LoadEnemies = new Queue<EnemyInfo>();

    public void FlushQueue()
    {
        
        LoadEquipItems.Clear();
        OtherPlayers.Clear();
        LoadEnemies.Clear();
    }
}

