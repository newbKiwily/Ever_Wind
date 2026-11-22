using System;
using System.IO;
using UnityEngine;

namespace EverWind.Gameplay
{
public enum RequestKind : ushort
{
    Join = 1,
    Move,
    Mode,
    Attack,
    Gather,
    Craft,
    Cancel,
    ChangeMap,
    Revive,
    Probe,
    UseItem,
    Equip,
    Unequip,
    ClaimQuest,
    Tutorial,
    ResourceRefill,
    Save,
    Logout,
    AttackTargets,
    UpgradeStat,
    UpgradeSkill,
    ResetStats,
    ResetSkills
}

public enum EventKind : ushort
{
    Ack = 1,
    Welcome,
    Spawn,
    Despawn,
    State,
    MoveResult,
    ActionStarted,
    ActionEnded,
    ActionCancelled,
    Hit,
    Inventory,
    Zone,
    SnapshotEnd,
    Stats,
    Equipment,
    Quest,
    Tutorial,
    Saved,
    Activity,
    Growth,
    SkillRank
}

public enum PlayerMode : ushort
{
    NonCombat,
    Combat,
    Looting,
    Dead
}

[System.Flags]
public enum HitReaction : ushort
{
    Knockback = 1,
    DamageOnly = 2,
    Hit = 4
}

public enum ActionKind : ushort
{
    None,
    Attack,
    Gather,
    Craft,
    Knockback
}

public enum EntityKind : ushort
{
    Player = 1,
    Monster,
    Resource,
    Wall,
    Boundary,
    Zone,
    Placement
}

public enum ResultCode : ushort
{
    None,
    Unauthenticated,
    BadRequest,
    WrongWorld,
    Stale,
    Dead,
    Restricted,
    OutOfRange,
    MissingTarget,
    Cooldown,
    Busy,
    Exhausted,
    Ingredients,
    Collision,
    Speed,
    DuplicateConflict,
    Expired,
    Cancelled,
    InsufficientPoints,
    RankLimit,
    UnlearnedSkill
}

public enum StatKind
{
    Attack = 1,
    Defence,
    MoveSpeed
}

public sealed class Request
{
    public RequestKind Kind;
    public ulong RequestId, ActionId;
    public uint Epoch, Revision, Sequence;
    public int Target, Definition;
    public Vector3 Position;
    public int[] Targets = Array.Empty<int>();
}

public sealed class ServerMessage
{
    public EventKind Kind;
    public ResultCode Error;
    public ushort Code, Flags;
    public int Entity, Target, Definition;
    public uint Epoch, Revision, Sequence;
    public ulong RequestId, ActionId;
    public double ServerTime;
    public Vector3 Position, Velocity;
    public float Hp, MaxHp, Radius, Duration, HitTime, Value;
    public PlayerMode Mode;
}

public interface IServerMessageHandler
{
    void Handle(ServerMessage message);
}

public static class Protocol
{
    public const ushort Version = 1, RequestPacket = 0x500, EventPacket = 0x501;
    public const int MaxAttackTargets = 64, RequestSize = 310, EventSize = 108;
}

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
public unsafe struct RequestPacket
{
    public ushort Version;
    public RequestKind Kind;
    public ulong RequestId;
    public uint Epoch, Revision, Sequence;
    public int Target, Definition;
    public ulong ActionId;
    public float X, Y, Z;
    public ushort TargetCount;
    public fixed int Targets[Protocol.MaxAttackTargets];
}

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
public struct EventPacket
{
    public ushort Version;
    public EventKind Kind;
    public ResultCode Error;
    public ushort Code;
    public int Entity, Target, Definition;
    public uint Epoch, Revision, Sequence;
    public ulong RequestId, ActionId;
    public double ServerTime;
    public float X, Y, Z, VX, VY, VZ;
    public float Hp, MaxHp, Radius, Duration, HitTime, Value;
    public PlayerMode Mode;
    public ushort Flags;
}
}
