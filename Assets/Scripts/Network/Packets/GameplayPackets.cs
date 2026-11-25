using System.Runtime.InteropServices;
using UnityEngine;

namespace EverWind.Gameplay
{
// 서버 Network/GameplayPackets.h와 동일한 필드 순서와 크기를 사용한다.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct JoinRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MoveRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ModeRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct AttackRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Target;
    public int Definition;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct GatherRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Target;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct CraftRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct CancelRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public ulong ActionId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ChangeMapRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ReviveRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ProbeRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Target;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UseItemRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
    public int Target;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct EquipRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UnequipRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ClaimQuestRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct TutorialRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ResourceRefillRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Target;
    public uint Sequence;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SaveRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct LogoutRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct AttackTargetsRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public ulong ActionId;
    public int Definition;
    public uint Sequence;
    public ushort TargetCount;
    public fixed int Targets[Protocol.MaxAttackTargets];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UpgradeStatRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UpgradeSkillRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ResetStatsRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ResetSkillsRequestPayload
{
    public ulong RequestId;
    public uint Epoch;
    public uint Revision;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct AckPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public ulong RequestId;
    public ulong ActionId;
    public ResultCode Error;
    public ushort Code;
    public Vector3 Position;
    public PlayerMode Mode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WelcomePayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public float Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SpawnPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ushort Code;
    public int Target;
    public float Duration;
    public float HitTime;
    public float Value;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct DespawnPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct StatePayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ushort Code;
    public int Target;
    public float Duration;
    public float HitTime;
    public float Value;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MoveResultPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ulong RequestId;
    public ResultCode Error;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ActionStartedPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ulong RequestId;
    public ulong ActionId;
    public ushort Code;
    public int Target;
    public float Duration;
    public float HitTime;
    public float Value;
    public ushort Flags;
    public ResultCode Error;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ActionEndedPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ulong RequestId;
    public ulong ActionId;
    public ushort Code;
    public int Target;
    public float Duration;
    public float HitTime;
    public float Value;
    public ushort Flags;
    public ResultCode Error;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ActionCancelledPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public ulong RequestId;
    public ulong ActionId;
    public ushort Code;
    public int Target;
    public float Duration;
    public float HitTime;
    public float Value;
    public ushort Flags;
    public ResultCode Error;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct HitPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public int Target;
    public ulong ActionId;
    public ulong RequestId;
    public float Value;
    public float Duration;
    public float HitTime;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct InventoryPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public int Definition;
    public float Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct ZonePayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public int Target;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SnapshotEndPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct StatsPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Hp;
    public float MaxHp;
    public float Radius;
    public int Definition;
    public PlayerMode Mode;
    public int Target;
    public float Value;
    public float Duration;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct EquipmentPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public ushort Code;
    public int Definition;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct QuestPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public int Definition;
    public ushort Code;
    public float Value;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct TutorialPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public int Definition;
    public ushort Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SavedPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public ulong RequestId;
    public ushort Code;
    public ResultCode Error;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct GrowthPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public ushort Code;
    public int Target;
    public int Definition;
    public float Value;
    public float Radius;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SkillRankPayload
{
    public int Entity;
    public uint Epoch;
    public uint Revision;
    public uint Sequence;
    public double ServerTime;
    public int Definition;
    public ushort Code;
    public float Value;
    public float Duration;
    public float HitTime;
}

}
