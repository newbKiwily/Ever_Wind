using System;
using System.IO;
using EverWind.Gameplay;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static unsafe class PacketMethod
{


    private static void CopyStringToFixed(string src, byte *dest, int destSize)
    {
        byte[] srcBytes = Encoding.UTF8.GetBytes(src);
        int copyLen = Math.Min(srcBytes.Length, destSize);
        Marshal.Copy(srcBytes, 0, (IntPtr)dest, copyLen);
        for (int i = copyLen; i < destSize; i++)
            dest[i] = 0;
    }

    public static byte[] BuildLoginRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_LOGIN_REQ)];
        fixed(byte *ptr = buffer)
        {
            PacketHeader *h = (PacketHeader *)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_LOGIN_REQ;

            PKT_LOGIN_REQ *body = (PKT_LOGIN_REQ *)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }





    public static byte[] BuildSignupRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_SIGNUP_REQ)];
        fixed(byte *ptr = buffer)
        {
            PacketHeader *h = (PacketHeader *)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_SIGNUP_REQ;

            PKT_SIGNUP_REQ *body = (PKT_SIGNUP_REQ *)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }
    public static bool IsGameplayRequest(ushort id)
    {
        switch ((PacketType)id)
        {
        case PacketType.C2S_JOIN_REQUEST:
        case PacketType.C2S_MOVE_REQUEST:
        case PacketType.C2S_MODE_REQUEST:
        case PacketType.C2S_ATTACK_REQUEST:
        case PacketType.C2S_GATHER_REQUEST:
        case PacketType.C2S_CRAFT_REQUEST:
        case PacketType.C2S_CANCEL_REQUEST:
        case PacketType.C2S_CHANGE_MAP_REQUEST:
        case PacketType.C2S_REVIVE_REQUEST:
        case PacketType.C2S_PROBE_REQUEST:
        case PacketType.C2S_USE_ITEM_REQUEST:
        case PacketType.C2S_EQUIP_REQUEST:
        case PacketType.C2S_UNEQUIP_REQUEST:
        case PacketType.C2S_CLAIM_QUEST_REQUEST:
        case PacketType.C2S_TUTORIAL_REQUEST:
        case PacketType.C2S_RESOURCE_REFILL_REQUEST:
        case PacketType.C2S_SAVE_REQUEST:
        case PacketType.C2S_LOGOUT_REQUEST:
        case PacketType.C2S_ATTACK_TARGETS_REQUEST:
        case PacketType.C2S_UPGRADE_STAT_REQUEST:
        case PacketType.C2S_UPGRADE_SKILL_REQUEST:
        case PacketType.C2S_RESET_STATS_REQUEST:
        case PacketType.C2S_RESET_SKILLS_REQUEST:
            return true;
        default:
            return false;
        }
    }

    public static byte[] BuildGameplayRequest(Request request)
    {
        int count = request.Targets != null ? request.Targets.Length : 0;
        if (count > Protocol.MaxAttackTargets || count > 0 && request.Kind != RequestKind.AttackTargets)
            throw new InvalidDataException("공격 대상 목록 오류");
        switch (request.Kind)
        {
        case RequestKind.Join:
        {
            var payload = new JoinRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_JOIN_REQUEST, payload);
        }
        case RequestKind.Move:
        {
            var payload = new MoveRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Sequence = request.Sequence;
            payload.Position = request.Position;
            return Encode(PacketType.C2S_MOVE_REQUEST, payload);
        }
        case RequestKind.Mode:
        {
            var payload = new ModeRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_MODE_REQUEST, payload);
        }
        case RequestKind.Attack:
        {
            var payload = new AttackRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Target = request.Target;
            payload.Definition = request.Definition;
            payload.Position = request.Position;
            return Encode(PacketType.C2S_ATTACK_REQUEST, payload);
        }
        case RequestKind.Gather:
        {
            var payload = new GatherRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Target = request.Target;
            return Encode(PacketType.C2S_GATHER_REQUEST, payload);
        }
        case RequestKind.Craft:
        {
            var payload = new CraftRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_CRAFT_REQUEST, payload);
        }
        case RequestKind.Cancel:
        {
            var payload = new CancelRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.ActionId = request.ActionId;
            return Encode(PacketType.C2S_CANCEL_REQUEST, payload);
        }
        case RequestKind.ChangeMap:
        {
            var payload = new ChangeMapRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_CHANGE_MAP_REQUEST, payload);
        }
        case RequestKind.Revive:
        {
            var payload = new ReviveRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_REVIVE_REQUEST, payload);
        }
        case RequestKind.Probe:
        {
            var payload = new ProbeRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Target = request.Target;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_PROBE_REQUEST, payload);
        }
        case RequestKind.UseItem:
        {
            var payload = new UseItemRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            payload.Target = request.Target;
            payload.Position = request.Position;
            return Encode(PacketType.C2S_USE_ITEM_REQUEST, payload);
        }
        case RequestKind.Equip:
        {
            var payload = new EquipRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_EQUIP_REQUEST, payload);
        }
        case RequestKind.Unequip:
        {
            var payload = new UnequipRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_UNEQUIP_REQUEST, payload);
        }
        case RequestKind.ClaimQuest:
        {
            var payload = new ClaimQuestRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_CLAIM_QUEST_REQUEST, payload);
        }
        case RequestKind.Tutorial:
        {
            var payload = new TutorialRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_TUTORIAL_REQUEST, payload);
        }
        case RequestKind.ResourceRefill:
        {
            var payload = new ResourceRefillRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Target = request.Target;
            payload.Sequence = request.Sequence;
            return Encode(PacketType.C2S_RESOURCE_REFILL_REQUEST, payload);
        }
        case RequestKind.Save:
        {
            var payload = new SaveRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_SAVE_REQUEST, payload);
        }
        case RequestKind.Logout:
        {
            var payload = new LogoutRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_LOGOUT_REQUEST, payload);
        }
        case RequestKind.AttackTargets:
        {
            var payload = new AttackTargetsRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.ActionId = request.ActionId;
            payload.Definition = request.Definition;
            payload.Sequence = request.Sequence;
            payload.TargetCount = (ushort)count;
            for (int i = 0; i < count; ++i) payload.Targets[i] = request.Targets[i];
            return Encode(PacketType.C2S_ATTACK_TARGETS_REQUEST, payload);
        }
        case RequestKind.UpgradeStat:
        {
            var payload = new UpgradeStatRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_UPGRADE_STAT_REQUEST, payload);
        }
        case RequestKind.UpgradeSkill:
        {
            var payload = new UpgradeSkillRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            payload.Definition = request.Definition;
            return Encode(PacketType.C2S_UPGRADE_SKILL_REQUEST, payload);
        }
        case RequestKind.ResetStats:
        {
            var payload = new ResetStatsRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_RESET_STATS_REQUEST, payload);
        }
        case RequestKind.ResetSkills:
        {
            var payload = new ResetSkillsRequestPayload();
            payload.RequestId = request.RequestId;
            payload.Epoch = request.Epoch;
            payload.Revision = request.Revision;
            return Encode(PacketType.C2S_RESET_SKILLS_REQUEST, payload);
        }
        default:
            throw new InvalidDataException("지원하지 않는 요청 종류");
        }
    }

    private static byte[] Encode<T>(PacketType type, T payload) where T : unmanaged
    {
        byte[] bytes = new byte[sizeof(PacketHeader) + sizeof(T)];
        fixed (byte* pointer = bytes)
        {
            *(PacketHeader*)pointer = new PacketHeader { Length = (ushort)bytes.Length, Id = (ushort)type };
            *(T*)(pointer + sizeof(PacketHeader)) = payload;
        }
        return bytes;
    }

    private static T ReadPayload<T>(byte[] bytes) where T : unmanaged
    {
        if (bytes == null || bytes.Length != sizeof(T))
            throw new InvalidDataException("패킷 본문 크기 오류: " + typeof(T).Name);
        fixed (byte* pointer = bytes) return *(T*)pointer;
    }

    public static void HandlePacket(NetworkClient transport, int generation, ushort id, byte[] payload)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("패킷은 리틀 엔디언을 사용합니다.");
        }

        switch ((PacketType)id)
        {
        case PacketType.S2C_LOGIN_ACK:
            if (payload.Length != sizeof(PKT_LOGIN_ACK))
            {
                throw new InvalidDataException("로그인 응답 크기 오류");
            }

            PKT_LOGIN_ACK login;

            fixed(byte *pointer = payload)
            {
                login = *(PKT_LOGIN_ACK *)pointer;
            }

            UnityMainThreadDispatcher.EnqueueWork(() =>
                                                  {
                                                      if (transport.IsCurrent(generation))
                                                      {
                                                          HandleLoginAck(transport, login);
                                                      }
                                                  });
            break;

        case PacketType.S2C_SIGNUP_ACK:
            if (payload.Length != sizeof(PKT_SIGNUP_ACK))
            {
                throw new InvalidDataException("가입 응답 크기 오류");
            }

            PKT_SIGNUP_ACK signup;

            fixed(byte *pointer = payload)
            {
                signup = *(PKT_SIGNUP_ACK *)pointer;
            }

            UnityMainThreadDispatcher.EnqueueWork(() =>
                                                  {
                                                      if (transport.IsCurrent(generation))
                                                      {
                                                          HandleSignupAck(transport, signup);
                                                      }
                                                  });
            break;

        case PacketType.S2C_ACK:
            var ack = ReadPayload<AckPayload>(payload);
            var messageAck = new ServerMessage { Kind = EventKind.Ack, Entity = ack.Entity, Epoch = ack.Epoch, Revision = ack.Revision, Sequence = ack.Sequence, ServerTime = ack.ServerTime, RequestId = ack.RequestId, ActionId = ack.ActionId, Error = ack.Error, Code = ack.Code, Position = ack.Position, Mode = ack.Mode };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleAckPacket(transport, messageAck);
            });
            break;

        case PacketType.S2C_WELCOME:
            var welcome = ReadPayload<WelcomePayload>(payload);
            var messageWelcome = new ServerMessage { Kind = EventKind.Welcome, Entity = welcome.Entity, Epoch = welcome.Epoch, Revision = welcome.Revision, Sequence = welcome.Sequence, ServerTime = welcome.ServerTime, Position = welcome.Position, Velocity = welcome.Velocity, Hp = welcome.Hp, MaxHp = welcome.MaxHp, Radius = welcome.Radius, Definition = welcome.Definition, Mode = welcome.Mode, Value = welcome.Value };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleWelcomePacket(transport, messageWelcome);
            });
            break;

        case PacketType.S2C_SPAWN:
            var spawn = ReadPayload<SpawnPayload>(payload);
            var messageSpawn = new ServerMessage { Kind = EventKind.Spawn, Entity = spawn.Entity, Epoch = spawn.Epoch, Revision = spawn.Revision, Sequence = spawn.Sequence, ServerTime = spawn.ServerTime, Position = spawn.Position, Velocity = spawn.Velocity, Hp = spawn.Hp, MaxHp = spawn.MaxHp, Radius = spawn.Radius, Definition = spawn.Definition, Mode = spawn.Mode, Code = spawn.Code, Target = spawn.Target, Duration = spawn.Duration, HitTime = spawn.HitTime, Value = spawn.Value, Flags = spawn.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleSpawnPacket(transport, messageSpawn);
            });
            break;

        case PacketType.S2C_DESPAWN:
            var despawn = ReadPayload<DespawnPayload>(payload);
            var messageDespawn = new ServerMessage { Kind = EventKind.Despawn, Entity = despawn.Entity, Epoch = despawn.Epoch, Revision = despawn.Revision, Sequence = despawn.Sequence, ServerTime = despawn.ServerTime };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleDespawnPacket(transport, messageDespawn);
            });
            break;

        case PacketType.S2C_STATE:
            var state = ReadPayload<StatePayload>(payload);
            var messageState = new ServerMessage { Kind = EventKind.State, Entity = state.Entity, Epoch = state.Epoch, Revision = state.Revision, Sequence = state.Sequence, ServerTime = state.ServerTime, Position = state.Position, Velocity = state.Velocity, Hp = state.Hp, MaxHp = state.MaxHp, Radius = state.Radius, Definition = state.Definition, Mode = state.Mode, Code = state.Code, Target = state.Target, Duration = state.Duration, HitTime = state.HitTime, Value = state.Value, Flags = state.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleStatePacket(transport, messageState);
            });
            break;

        case PacketType.S2C_MOVE_RESULT:
            var moveresult = ReadPayload<MoveResultPayload>(payload);
            var messageMoveResult = new ServerMessage { Kind = EventKind.MoveResult, Entity = moveresult.Entity, Epoch = moveresult.Epoch, Revision = moveresult.Revision, Sequence = moveresult.Sequence, ServerTime = moveresult.ServerTime, Position = moveresult.Position, Velocity = moveresult.Velocity, Hp = moveresult.Hp, MaxHp = moveresult.MaxHp, Radius = moveresult.Radius, Definition = moveresult.Definition, Mode = moveresult.Mode, RequestId = moveresult.RequestId, Error = moveresult.Error };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleMoveResultPacket(transport, messageMoveResult);
            });
            break;

        case PacketType.S2C_ACTION_STARTED:
            var actionstarted = ReadPayload<ActionStartedPayload>(payload);
            var messageActionStarted = new ServerMessage { Kind = EventKind.ActionStarted, Entity = actionstarted.Entity, Epoch = actionstarted.Epoch, Revision = actionstarted.Revision, Sequence = actionstarted.Sequence, ServerTime = actionstarted.ServerTime, Position = actionstarted.Position, Velocity = actionstarted.Velocity, Hp = actionstarted.Hp, MaxHp = actionstarted.MaxHp, Radius = actionstarted.Radius, Definition = actionstarted.Definition, Mode = actionstarted.Mode, RequestId = actionstarted.RequestId, ActionId = actionstarted.ActionId, Code = actionstarted.Code, Target = actionstarted.Target, Duration = actionstarted.Duration, HitTime = actionstarted.HitTime, Value = actionstarted.Value, Flags = actionstarted.Flags, Error = actionstarted.Error };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleActionStartedPacket(transport, messageActionStarted);
            });
            break;

        case PacketType.S2C_ACTION_ENDED:
            var actionended = ReadPayload<ActionEndedPayload>(payload);
            var messageActionEnded = new ServerMessage { Kind = EventKind.ActionEnded, Entity = actionended.Entity, Epoch = actionended.Epoch, Revision = actionended.Revision, Sequence = actionended.Sequence, ServerTime = actionended.ServerTime, Position = actionended.Position, Velocity = actionended.Velocity, Hp = actionended.Hp, MaxHp = actionended.MaxHp, Radius = actionended.Radius, Definition = actionended.Definition, Mode = actionended.Mode, RequestId = actionended.RequestId, ActionId = actionended.ActionId, Code = actionended.Code, Target = actionended.Target, Duration = actionended.Duration, HitTime = actionended.HitTime, Value = actionended.Value, Flags = actionended.Flags, Error = actionended.Error };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleActionEndedPacket(transport, messageActionEnded);
            });
            break;

        case PacketType.S2C_ACTION_CANCELLED:
            var actioncancelled = ReadPayload<ActionCancelledPayload>(payload);
            var messageActionCancelled = new ServerMessage { Kind = EventKind.ActionCancelled, Entity = actioncancelled.Entity, Epoch = actioncancelled.Epoch, Revision = actioncancelled.Revision, Sequence = actioncancelled.Sequence, ServerTime = actioncancelled.ServerTime, Position = actioncancelled.Position, Velocity = actioncancelled.Velocity, Hp = actioncancelled.Hp, MaxHp = actioncancelled.MaxHp, Radius = actioncancelled.Radius, Definition = actioncancelled.Definition, Mode = actioncancelled.Mode, RequestId = actioncancelled.RequestId, ActionId = actioncancelled.ActionId, Code = actioncancelled.Code, Target = actioncancelled.Target, Duration = actioncancelled.Duration, HitTime = actioncancelled.HitTime, Value = actioncancelled.Value, Flags = actioncancelled.Flags, Error = actioncancelled.Error };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleActionCancelledPacket(transport, messageActionCancelled);
            });
            break;

        case PacketType.S2C_HIT:
            var hit = ReadPayload<HitPayload>(payload);
            var messageHit = new ServerMessage { Kind = EventKind.Hit, Entity = hit.Entity, Epoch = hit.Epoch, Revision = hit.Revision, Sequence = hit.Sequence, ServerTime = hit.ServerTime, Position = hit.Position, Velocity = hit.Velocity, Hp = hit.Hp, MaxHp = hit.MaxHp, Radius = hit.Radius, Definition = hit.Definition, Mode = hit.Mode, Target = hit.Target, ActionId = hit.ActionId, RequestId = hit.RequestId, Value = hit.Value, Duration = hit.Duration, HitTime = hit.HitTime, Flags = hit.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleHitPacket(transport, messageHit);
            });
            break;

        case PacketType.S2C_INVENTORY:
            var inventory = ReadPayload<InventoryPayload>(payload);
            var messageInventory = new ServerMessage { Kind = EventKind.Inventory, Entity = inventory.Entity, Epoch = inventory.Epoch, Revision = inventory.Revision, Sequence = inventory.Sequence, ServerTime = inventory.ServerTime, Definition = inventory.Definition, Value = inventory.Value };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleInventoryPacket(transport, messageInventory);
            });
            break;

        case PacketType.S2C_ZONE:
            var zone = ReadPayload<ZonePayload>(payload);
            var messageZone = new ServerMessage { Kind = EventKind.Zone, Entity = zone.Entity, Epoch = zone.Epoch, Revision = zone.Revision, Sequence = zone.Sequence, ServerTime = zone.ServerTime, Target = zone.Target, Flags = zone.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleZonePacket(transport, messageZone);
            });
            break;

        case PacketType.S2C_SNAPSHOT_END:
            var snapshotend = ReadPayload<SnapshotEndPayload>(payload);
            var messageSnapshotEnd = new ServerMessage { Kind = EventKind.SnapshotEnd, Entity = snapshotend.Entity, Epoch = snapshotend.Epoch, Revision = snapshotend.Revision, Sequence = snapshotend.Sequence, ServerTime = snapshotend.ServerTime };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleSnapshotEndPacket(transport, messageSnapshotEnd);
            });
            break;

        case PacketType.S2C_STATS:
            var stats = ReadPayload<StatsPayload>(payload);
            var messageStats = new ServerMessage { Kind = EventKind.Stats, Entity = stats.Entity, Epoch = stats.Epoch, Revision = stats.Revision, Sequence = stats.Sequence, ServerTime = stats.ServerTime, Position = stats.Position, Velocity = stats.Velocity, Hp = stats.Hp, MaxHp = stats.MaxHp, Radius = stats.Radius, Definition = stats.Definition, Mode = stats.Mode, Target = stats.Target, Value = stats.Value, Duration = stats.Duration };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleStatsPacket(transport, messageStats);
            });
            break;

        case PacketType.S2C_EQUIPMENT:
            var equipment = ReadPayload<EquipmentPayload>(payload);
            var messageEquipment = new ServerMessage { Kind = EventKind.Equipment, Entity = equipment.Entity, Epoch = equipment.Epoch, Revision = equipment.Revision, Sequence = equipment.Sequence, ServerTime = equipment.ServerTime, Code = equipment.Code, Definition = equipment.Definition };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleEquipmentPacket(transport, messageEquipment);
            });
            break;

        case PacketType.S2C_QUEST:
            var quest = ReadPayload<QuestPayload>(payload);
            var messageQuest = new ServerMessage { Kind = EventKind.Quest, Entity = quest.Entity, Epoch = quest.Epoch, Revision = quest.Revision, Sequence = quest.Sequence, ServerTime = quest.ServerTime, Definition = quest.Definition, Code = quest.Code, Value = quest.Value, Flags = quest.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleQuestPacket(transport, messageQuest);
            });
            break;

        case PacketType.S2C_TUTORIAL:
            var tutorial = ReadPayload<TutorialPayload>(payload);
            var messageTutorial = new ServerMessage { Kind = EventKind.Tutorial, Entity = tutorial.Entity, Epoch = tutorial.Epoch, Revision = tutorial.Revision, Sequence = tutorial.Sequence, ServerTime = tutorial.ServerTime, Definition = tutorial.Definition, Flags = tutorial.Flags };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleTutorialPacket(transport, messageTutorial);
            });
            break;

        case PacketType.S2C_SAVED:
            var saved = ReadPayload<SavedPayload>(payload);
            var messageSaved = new ServerMessage { Kind = EventKind.Saved, Entity = saved.Entity, Epoch = saved.Epoch, Revision = saved.Revision, Sequence = saved.Sequence, ServerTime = saved.ServerTime, RequestId = saved.RequestId, Code = saved.Code, Error = saved.Error };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleSavedPacket(transport, messageSaved);
            });
            break;

        case PacketType.S2C_GROWTH:
            var growth = ReadPayload<GrowthPayload>(payload);
            var messageGrowth = new ServerMessage { Kind = EventKind.Growth, Entity = growth.Entity, Epoch = growth.Epoch, Revision = growth.Revision, Sequence = growth.Sequence, ServerTime = growth.ServerTime, Code = growth.Code, Target = growth.Target, Definition = growth.Definition, Value = growth.Value, Radius = growth.Radius, Position = growth.Position };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleGrowthPacket(transport, messageGrowth);
            });
            break;

        case PacketType.S2C_SKILL_RANK:
            var skillrank = ReadPayload<SkillRankPayload>(payload);
            var messageSkillRank = new ServerMessage { Kind = EventKind.SkillRank, Entity = skillrank.Entity, Epoch = skillrank.Epoch, Revision = skillrank.Revision, Sequence = skillrank.Sequence, ServerTime = skillrank.ServerTime, Definition = skillrank.Definition, Code = skillrank.Code, Value = skillrank.Value, Duration = skillrank.Duration, HitTime = skillrank.HitTime };
            UnityMainThreadDispatcher.EnqueueWork(() =>
            {
                if (transport.IsCurrent(generation)) HandleSkillRankPacket(transport, messageSkillRank);
            });
            break;

        // 인증 시 함께 오는 과거 초기 데이터는 현재 스냅샷으로 대체한다.
        case PacketType._INVENTORYITEM:
        case PacketType._PLAYERSTAT:
        case PacketType.S2C_QUEST_INFO:
            break;

        default:
            throw new InvalidDataException("지원하지 않는 패킷 ID: " + id);
        }
    }

    public static void HandleLoginAck(NetworkClient transport, PKT_LOGIN_ACK packet)
    {
        transport.CompleteLogin(packet.ResultCode, packet.UserDBID);
    }

    public static void HandleSignupAck(NetworkClient transport, PKT_SIGNUP_ACK packet)
    {
        transport.CompleteSignup(packet.ResultCode);
    }

    public static void HandleAckPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleWelcomePacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleSpawnPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleDespawnPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleStatePacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleMoveResultPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleActionStartedPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleActionEndedPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleActionCancelledPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleHitPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleInventoryPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleZonePacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleSnapshotEndPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleStatsPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleEquipmentPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleQuestPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleTutorialPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleSavedPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleGrowthPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void HandleSkillRankPacket(NetworkClient transport, ServerMessage message)
    {
        if (transport.Accept(message)) ContainerInMap.Current.Receive(message);
    }

    public static void ApplyWorldPacket(ServerMessage message)
    {
        var gameplay = GameplayIntegration.Active;
        if (gameplay == null) throw new InvalidOperationException("게임 처리 객체가 준비되지 않았습니다.");
        gameplay.Registry.Receive(message);
        gameplay.Receive(message);
    }
}
