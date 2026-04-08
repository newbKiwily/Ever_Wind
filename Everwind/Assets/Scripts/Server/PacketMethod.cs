using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static unsafe class PacketMethod
{
    private static string GetStringFromFixed(byte* ptr, int size)
    {
        return Encoding.UTF8.GetString(ptr, size).TrimEnd('\0');
    }

    private static void CopyStringToFixed(string src, byte* dest, int destSize)
    {
        byte[] srcBytes = Encoding.UTF8.GetBytes(src);
        int copyLen = Math.Min(srcBytes.Length, destSize);
        Marshal.Copy(srcBytes, 0, (IntPtr)dest, copyLen);
        for (int i = copyLen; i < destSize; i++) dest[i] = 0;
    }

    public static byte[] BuildLoginRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_LOGIN_REQ)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_LOGIN_REQ;

            PKT_LOGIN_REQ* body = (PKT_LOGIN_REQ*)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }

    public static byte[] BuildInventoryPkt(int userDbId, DataCenter.InventoryData item)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_INVENTORY_ITEM)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._INVENTORYITEM;

            PKT_INVENTORY_ITEM* body = (PKT_INVENTORY_ITEM*)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(item.Key, body->ItemId, 32);
            body->Amount = item.Amount;
            body->IsEquipped = item.IsEquppiedItem;
            body->SlotIndex = item.SlotIndex;
        }
        return buffer;
    }

    public static byte[] BuildStatPkt(int userDbId, PlayerStatManager.Stat stat)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_USERSTAT)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._PLAYERSTAT;

            PKT_USERSTAT* body = (PKT_USERSTAT*)(ptr + sizeof(PacketHeader));
            body->AttackPower = stat.AttackPower;
            body->DefecnePower = stat.DefencePower;
            body->Hp = stat.Hp;
            body->MaxHp = stat.MaxHp;
            body->Speed = stat.Speed;
        }
        return buffer;
    }

    public static byte[] BuildSignupRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_SIGNUP_REQ)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_SIGNUP_REQ;

            PKT_SIGNUP_REQ* body = (PKT_SIGNUP_REQ*)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }

    public static byte[] BuildMoveSyncRequest(int userDbId, Vector3 position, float speed, uint timestamp)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_MOVE_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_MOVESYNC_REQ;

            PKT_MOVE_SYNC* body = (PKT_MOVE_SYNC*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->PosX = position.x;
            body->PosY = position.y;
            body->PosZ = position.z;
            body->Speed = speed;
            body->Timestamp = timestamp;
        }
        return buffer;
    }

    public static byte[] BuildAttackReq(int instanceId, float damage)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_C2S_ATTACK_REQ)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_ATTACK_REQ;

            PKT_C2S_ATTACK_REQ* body = (PKT_C2S_ATTACK_REQ*)(ptr + sizeof(PacketHeader));
            body->InstanceId = instanceId;
            body->Damage = damage;
        }
        return buffer;
    }

    public static byte[] BuildEnemyMoveSync(int instanceId, Vector3 position)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_ENEMY_MOVE_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_ENEMY_MOVE_SYNC;

            PKT_ENEMY_MOVE_SYNC* body = (PKT_ENEMY_MOVE_SYNC*)(ptr + sizeof(PacketHeader));
            body->InstanceId = instanceId;
            body->PosX = position.x;
            body->PosY = position.y;
            body->PosZ = position.z;
        }
        return buffer;
    }

    public static byte[] BuildOneshotAnimReq(int userDbId, OneshotAnimKey animKey)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_ONESHOT_ANIM_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._ONESHOT_ANIM_SYNC;

            PKT_ONESHOT_ANIM_SYNC* body = (PKT_ONESHOT_ANIM_SYNC*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->AnimCode = (int)animKey;
        }
        return buffer;
    }

    public static byte[] BuildInteractSyncReq(int userDbId, bool isStart)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_INTERACT_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._INTERACT_SYNC;

            PKT_INTERACT_SYNC* body = (PKT_INTERACT_SYNC*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->IsStart = isStart ? (byte)1 : (byte)0;
        }
        return buffer;
    }

    public static byte[] BuildDeadSyncReq(int userDbId, bool isStart)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_DEAD_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._DEAD_SYNC;

            PKT_DEAD_SYNC* body = (PKT_DEAD_SYNC*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->IsStart = isStart ? (byte)1 : (byte)0;
        }
        return buffer;
    }

    public static byte[] BuildCombatStateSync(int userDbId, bool isCombat)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_COMBAT_STATE_SYNC)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType._COMBAT_STATE_SYNC;

            PKT_COMBAT_STATE_SYNC* body = (PKT_COMBAT_STATE_SYNC*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->IsCombat = isCombat ? (byte)1 : (byte)0;
        }
        return buffer;
    }

    public static byte[] BuildEnemyAttackAnim(int instanceId)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_ENEMY_ATTACK_ANIM)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_ENEMY_ATTACK_ANIM;

            PKT_ENEMY_ATTACK_ANIM* body = (PKT_ENEMY_ATTACK_ANIM*)(ptr + sizeof(PacketHeader));
            body->InstanceId = instanceId;
        }
        return buffer;
    }

    public static byte[] BuildEnemyDeadReq(int instanceId)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_C2S_ENEMY_DEAD_REQ)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_ENEMY_DEAD_REQ;

            PKT_C2S_ENEMY_DEAD_REQ* body = (PKT_C2S_ENEMY_DEAD_REQ*)(ptr + sizeof(PacketHeader));
            body->InstanceId = instanceId;
        }
        return buffer;
    }

    public static byte[] BuildMapChangeReq(int userDbId, int targetMapId)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_MAP_CHANGE_REQ)];
        fixed (byte* ptr = buffer)
        {
            PacketHeader* h = (PacketHeader*)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_MAP_CHANGE_REQ;

            PKT_MAP_CHANGE_REQ* body = (PKT_MAP_CHANGE_REQ*)(ptr + sizeof(PacketHeader));
            body->UserDBID = userDbId;
            body->TargetMapId = targetMapId;
        }
        return buffer;
    }

    public static void HandleLoginAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_LOGIN_ACK* pkt = (PKT_LOGIN_ACK*)ptr;

            DataCenter.LoginData tempData = new DataCenter.LoginData();
            tempData.Position = new Vector3(pkt->PosX, pkt->PosY, pkt->PosZ);
            tempData.MapId = pkt->UserMapId;
            SingletonManager.Instance.GetSingleton<DataCenter>().loginData = tempData;

            int result = pkt->ResultCode;
            int userDbId = pkt->UserDBID;
            SingletonManager.Instance.GetSingleton<NetworkClient>().UserDbId = userDbId;
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                LoginUI ui = GameObject.FindObjectOfType<LoginUI>();
                if (ui == null) return;

                switch (result)
                {
                    case 0:
                        SingletonManager.Instance.GetSingleton<SceneLoader>().LoadGame("InGame");
                        break;
                    case 1: ui.SetResult("Wrong ID"); break;
                    case 2: ui.SetResult("Wrong Password"); break;
                    case 3: ui.SetResult("Account is Using"); break;
                    default: ui.SetResult("Unknown Error"); break;
                }
                ui.FlushTest();
            });
        }
    }

    public static void HandleSignupAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_SIGNUP_ACK* pkt = (PKT_SIGNUP_ACK*)ptr;
            int result = pkt->ResultCode;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                LoginUI ui = GameObject.FindObjectOfType<LoginUI>();
                if (ui == null) return;

                switch (result)
                {
                    case 0: ui.SetResult("회원가입을 완료하였습니다."); break;
                    case 1: ui.SetResult("이미 있는 아이디와 비밀번호 입니다."); break;
                    case 2: ui.SetResult("데이터베이스 오류입니다."); break;
                    default: ui.SetResult("Unknown Error"); break;
                }
                ui.FlushTest();
            });
        }
    }

    public static void HandleMoveSyncAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_MOVE_SYNC* pkt = (PKT_MOVE_SYNC*)ptr;

            int userDbId = pkt->UserDBID;
            Vector3 position = new Vector3(pkt->PosX, pkt->PosY, pkt->PosZ);
            float speed = pkt->Speed;
            uint timestamp = pkt->Timestamp;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var otherPlayerManager = SingletonManager.Instance.GetSingleton<OtherPlayerManager>();
                var targetPlayer = otherPlayerManager.FindPlayer(userDbId);
                if (targetPlayer == null)
                {
                    return;
                }

                targetPlayer.GetComponent<OtherPlayer>().OnMoveSync(position, speed, timestamp);
            });
        }
    }

    public static void HandlePlayerListAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_PLAYER_LIST_ACK* pkt = (PKT_PLAYER_LIST_ACK*)ptr;
            int userDbId = pkt->UserDBID;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var worldLoader = SingletonManager.Instance.GetSingleton<WorldLoader>();
                if (worldLoader != null && worldLoader.InstancedPlayer != null)
                {
                    worldLoader.SpawnOtherPlayers(userDbId);
                    return;
                }

                SingletonManager.Instance.GetSingleton<DataCenter>().OtherPlayers.Enqueue(userDbId);
            });
        }
    }

    public static void HandlePlayerListRuntime(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_PLAYER_LIST_RUNTIME_ACK* pkt = (PKT_PLAYER_LIST_RUNTIME_ACK*)ptr;
            int userDbId = pkt->UserDBID;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                SingletonManager.Instance.GetSingleton<WorldLoader>().SpawnOtherPlayers(userDbId);
            });
        }
    }

    public static void HandlePlayerLogoutAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_LOGOUT_ACK* pkt = (PKT_LOGOUT_ACK*)ptr;
            int targetId = pkt->UserDBID;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                SingletonManager.Instance.GetSingleton<OtherPlayerManager>().RemovePlayer(targetId);
            });
        }
    }

    public static void HandleInventoryAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_INVENTORY_ITEM* pkt = (PKT_INVENTORY_ITEM*)ptr;

            string itemId = GetStringFromFixed(pkt->ItemId, 32);
            int amount = pkt->Amount;
            int slotIndex = pkt->SlotIndex;
            int isEquipped = pkt->IsEquipped;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var dataCenter = SingletonManager.Instance.GetSingleton<DataCenter>();
                DataCenter.InventoryData inventoryData = new DataCenter.InventoryData
                {
                    Key = itemId,
                    Amount = amount,
                    SlotIndex = slotIndex,
                    IsEquppiedItem = isEquipped
                };

                if (isEquipped == 1)
                    dataCenter.LoadEquipItems.Enqueue(itemId);
                else
                    dataCenter.LoadItems.Add(inventoryData);
            });
        }
    }

    public static void HandlePlayerStatAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_USERSTAT* pkt = (PKT_USERSTAT*)ptr;

            PlayerStatManager.Stat stat = new PlayerStatManager.Stat();

            stat.AttackPower = pkt->AttackPower;
            stat.Hp = pkt->Hp;
            stat.MaxHp = pkt->MaxHp;
            stat.Speed = pkt->Speed;
            stat.DefencePower = pkt->DefecnePower;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                SingletonManager.Instance.GetSingleton<DataCenter>().LoadStat = stat;
            });
        }
    }

    public static void HandleEnemyInfo(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_ENEMY_SPAWN* pkt = (PKT_ENEMY_SPAWN*)ptr;

            DataCenter.EnemyInfo info = new DataCenter.EnemyInfo();

            info.InstanceId = pkt->InstanceId;
            info.EnemyId = pkt->EnemyId;
            info.Position.x = pkt->PosX;
            info.Position.y = pkt->PosY;
            info.Position.z = pkt->PosZ;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var worldLoader = SingletonManager.Instance.GetSingleton<WorldLoader>();
                if (worldLoader != null && worldLoader.InstancedPlayer != null)
                {
                    worldLoader.SpawnEnemy(info);
                }
                else
                {
                    SingletonManager.Instance.GetSingleton<DataCenter>().LoadEnemies.Enqueue(info);
                }
            });
        }
    }

    public static void HandleEnemyDamaged(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_S2C_ENEMY_DAMAGED* pkt = (PKT_S2C_ENEMY_DAMAGED*)ptr;

            int instanceId = pkt->InstanceId;
            float currentHp = pkt->CurrentHp;
            float damageAmount = pkt->DamageAmount;
            int ownerDbId = pkt->OwnerDbId;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var spawner = SingletonManager.Instance.GetSingleton<EnemySpawner>();
                if (spawner != null)
                {
                    Enemy targetEnemy = spawner.FindEnemy(instanceId);
                    if (targetEnemy != null)
                    {
                        var syncTarget = targetEnemy.GetComponent<EnemyNetworkSync>();
                        if (syncTarget != null)
                        {
                            syncTarget.OnReceiveDamaged(currentHp, damageAmount, ownerDbId);
                        }
                    }
                }
            });
        }
    }

    public static void HandleEnemyMoveSync(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_ENEMY_MOVE_SYNC* pkt = (PKT_ENEMY_MOVE_SYNC*)ptr;

            int instanceId = pkt->InstanceId;
            Vector3 newPos = new Vector3(pkt->PosX, pkt->PosY, pkt->PosZ);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var spawner = SingletonManager.Instance.GetSingleton<EnemySpawner>();
                if (spawner != null)
                {
                    Enemy targetEnemy = spawner.FindEnemy(instanceId);
                    if (targetEnemy != null)
                    {
                        var syncTarget = targetEnemy.GetComponent<EnemyNetworkSync>();
                        if (syncTarget != null)
                        {
                            syncTarget.OnReceiveMoveSync(newPos);
                        }
                    }
                }
            });
        }
    }

    public static void HandleEnemyAttackAnim(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_ENEMY_ATTACK_ANIM* pkt = (PKT_ENEMY_ATTACK_ANIM*)ptr;
            int instanceId = pkt->InstanceId;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var spawner = SingletonManager.Instance.GetSingleton<EnemySpawner>();
                if (spawner != null)
                {
                    Enemy targetEnemy = spawner.FindEnemy(instanceId);
                    if (targetEnemy != null)
                    {
                        var syncTarget = targetEnemy.GetComponent<EnemyNetworkSync>();
                        if (syncTarget != null)
                        {
                            syncTarget.OnReceiveAttackAnim();
                        }
                    }
                }
            });
        }
    }

    public static void HandleOneshotAnimSync(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_ONESHOT_ANIM_SYNC* pkt = (PKT_ONESHOT_ANIM_SYNC*)ptr;
            int targetUserDbId = pkt->UserDBID;
            int animCode = pkt->AnimCode;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var targetPlayer = SingletonManager.Instance.GetSingleton<OtherPlayerManager>().FindPlayer(targetUserDbId);
                if (targetPlayer != null) targetPlayer.GetComponent<OtherPlayerNetworkSync>().OnReceiveOneshot(animCode);
            });
        }
    }

    public static void HandleInteractSync(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_INTERACT_SYNC* pkt = (PKT_INTERACT_SYNC*)ptr;
            int targetUserDbId = pkt->UserDBID;
            bool isStart = pkt->IsStart == 1;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var targetPlayer = SingletonManager.Instance.GetSingleton<OtherPlayerManager>().FindPlayer(targetUserDbId);
                if (targetPlayer != null)
                    targetPlayer.GetComponent<OtherPlayerNetworkSync>().OnReceiveInteract(isStart);
            });
        }
    }

    public static void HandleDeadSync(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_DEAD_SYNC* pkt = (PKT_DEAD_SYNC*)ptr;
            int targetUserDbId = pkt->UserDBID;
            bool isStart = pkt->IsStart == 1;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var targetPlayer = SingletonManager.Instance.GetSingleton<OtherPlayerManager>().FindPlayer(targetUserDbId);
                if (targetPlayer != null)
                    targetPlayer.GetComponent<OtherPlayerNetworkSync>().OnReceiveDead(isStart);
            });
        }
    }

    public static void HandleCombatStateSync(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_COMBAT_STATE_SYNC* pkt = (PKT_COMBAT_STATE_SYNC*)ptr;
            int targetUserDbId = pkt->UserDBID;
            bool isCombat = pkt->IsCombat == 1;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var targetPlayer = SingletonManager.Instance.GetSingleton<OtherPlayerManager>().FindPlayer(targetUserDbId);

                if (targetPlayer != null)
                {
                    targetPlayer.GetComponent<OtherPlayerNetworkSync>().SetIsNormal(isCombat);
                    if (isCombat)
                    {
                        targetPlayer.OnWeapon();
                    }
                    else
                    {
                        targetPlayer.OffWeapon();
                    }
                }
            });
        }
    }

    public static void HandleMapChangeAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_MAP_CHANGE_ACK* pkt = (PKT_MAP_CHANGE_ACK*)ptr;
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var dataCenter = SingletonManager.Instance.GetSingleton<DataCenter>();
               
                dataCenter.loginData = new DataCenter.LoginData
                {
                    MapId = pkt->MapId,
                    Position = new Vector3(pkt->PosX, pkt->PosY, pkt->PosZ)
                };

                var worldLoader = SingletonManager.Instance.GetSingleton<WorldLoader>();
                worldLoader.ChangeWorld(
                    dataCenter.loginData.MapId,
                    dataCenter.loginData.Position,
                    dataCenter.OtherPlayers,
                    dataCenter.LoadEnemies
                );
            });
        }
    }

    public static void HandleEnemyDeadAck(byte[] payload)
    {
        fixed (byte* ptr = payload)
        {
            PKT_S2C_ENEMY_DEAD_ACK* pkt = (PKT_S2C_ENEMY_DEAD_ACK*)ptr;
            int instanceId = pkt->InstanceId;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var spawner = SingletonManager.Instance.GetSingleton<EnemySpawner>();
                if (spawner != null)
                {
                    Enemy targetEnemy = spawner.FindEnemy(instanceId);
                    if (targetEnemy != null)
                    {
                        var syncTarget = targetEnemy.GetComponent<EnemyNetworkSync>();
                        if (syncTarget != null)
                        {
                            syncTarget.OnReceiveDeadAck();
                        }
                        else
                        {
                            targetEnemy.Die();
                        }
                    }
                }
            });
        }
    }
}

