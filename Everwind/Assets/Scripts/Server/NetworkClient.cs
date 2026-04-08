using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkClient : SingletonBase<NetworkClient>
{
    public override bool IsPersistent => true;

    private TcpClient _client;
    public NetworkStream Stream;
    private Thread _recvThread;
    public int UserDbId;

    protected override void Awake()
    {
        Application.runInBackground = true;
        Priority = -90;
        base.Awake();
        Application.wantsToQuit += Logout;
    }

    private void Start()
    {
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        try
        {
            _client = new TcpClient("127.0.0.1", 4000);
            Stream = _client.GetStream();

            _recvThread = new Thread(ReceiveLoop);
            _recvThread.IsBackground = true;
            _recvThread.Start();

            Debug.Log("[NetworkClient] Connected to server.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Connection failed: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        try { _recvThread?.Abort(); } catch { }
        try { Stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
    }

    private void ReceiveLoop()
    {
        try
        {
            while (true)
            {
                byte[] header = new byte[4];
                ReadExact(Stream, header, 4);

                ushort totalLength = BitConverter.ToUInt16(header, 0);
                ushort packetId = BitConverter.ToUInt16(header, 2);

                int payloadLength = totalLength - 4;

                byte[] payload = new byte[payloadLength];
                ReadExact(Stream, payload, payloadLength);

                HandlePacket((PacketType)packetId, payload);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("[NetworkClient] Server disconnected: " + ex.Message);
        }
    }

    private void ReadExact(NetworkStream s, byte[] buf, int len)
    {
        int read = 0;
        while (read < len)
        {
            int r = s.Read(buf, read, len - read);
            if (r <= 0) throw new Exception("Disconnected");
            read += r;
        }
    }

    public bool Logout()
    {
        var itemList = SingletonManager.Instance.GetSingleton<PopUpUIManager>().Inventory.GetCurrentInventoryData();
        var stat = SingletonManager.Instance.GetSingleton<WorldLoader>().InstancedPlayer.GetComponent<PlayerStatManager>().GetStat();
        foreach (var item in itemList)
        {
            var itemPkt = PacketMethod.BuildInventoryPkt(UserDbId, item);
            Send(itemPkt);
        }

        var statPkt = PacketMethod.BuildStatPkt(UserDbId, stat);
        Send(statPkt);

        return true;
    }

    private void HandlePacket(PacketType type, byte[] payload)
    {
        switch (type)
        {
            case PacketType.S2C_LOGIN_ACK:
                PacketMethod.HandleLoginAck(payload);
                break;
            case PacketType.S2C_SIGNUP_ACK:
                PacketMethod.HandleSignupAck(payload);
                break;
            case PacketType.S2C_MOVESYNC_ACK:
                PacketMethod.HandleMoveSyncAck(payload);
                break;
            case PacketType.S2C_PLAYERLIST_ACK:
                PacketMethod.HandlePlayerListAck(payload);
                break;
            case PacketType.S2C_LOGOUT_ACK:
                PacketMethod.HandlePlayerLogoutAck(payload);
                break;
            case PacketType._INVENTORYITEM:
                PacketMethod.HandleInventoryAck(payload);
                break;
            case PacketType._PLAYERSTAT:
                PacketMethod.HandlePlayerStatAck(payload);
                break;
            case PacketType.S2C_PLAYERLIST_RUNTIME:
                PacketMethod.HandlePlayerListRuntime(payload);
                break;
            case PacketType.S2C_ENEMY_SPAWN:
                PacketMethod.HandleEnemyInfo(payload);
                break;
            case PacketType.S2C_ENEMY_DAMAGED:
                PacketMethod.HandleEnemyDamaged(payload);
                break;
            case PacketType.S2C_ENEMY_MOVE_SYNC:
                PacketMethod.HandleEnemyMoveSync(payload);
                break;
            case PacketType.S2C_ENEMY_DEAD_ACK:
                PacketMethod.HandleEnemyDeadAck(payload);
                break;
            case PacketType.S2C_ENEMY_ATTACK_ANIM:
                PacketMethod.HandleEnemyAttackAnim(payload);
                break;
            case PacketType._ONESHOT_ANIM_SYNC:
                PacketMethod.HandleOneshotAnimSync(payload);
                break;
            case PacketType._INTERACT_SYNC:
                PacketMethod.HandleInteractSync(payload);
                break;
            case PacketType._DEAD_SYNC:
                PacketMethod.HandleDeadSync(payload);
                break;
            case PacketType._COMBAT_STATE_SYNC:
                PacketMethod.HandleCombatStateSync(payload);
                break;
            case PacketType.S2C_MAP_CHANGE_ACK:
                PacketMethod.HandleMapChangeAck(payload);
                break;
            default:
                Debug.LogWarning("Unknown packet: " + type);
                break;
        }
    }

    public void Send(byte[] packet)
    {
        if (Stream == null)
            return;

        try
        {
            Stream.Write(packet, 0, packet.Length);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Send failed: {ex.Message}");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            SingletonManager.Instance.GetSingleton<DataCenter>().FlushQueue();
            var pkt = PacketMethod.BuildMapChangeReq(UserDbId, 1);
            Send(pkt);
        }
        if(Input.GetKeyDown(KeyCode.B))
        {
            SingletonManager.Instance.GetSingleton<DataCenter>().FlushQueue();
            var pkt = PacketMethod.BuildMapChangeReq(UserDbId, 0);
            Send(pkt);
        }
    }
}

