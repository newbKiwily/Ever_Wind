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
    Logout
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
    Activity
}
public enum PlayerMode : ushort
{
    NonCombat,
    Combat,
    Looting,
    Dead
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
    Cancelled
}
public sealed class Request
{
    public RequestKind Kind;
    public ulong RequestId, ActionId;
    public uint Epoch, Revision, Sequence;
    public int Target, Definition;
    public Vector3 Position;
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
    public const int RequestSize = 52, EventSize = 108;
    public static byte[] Encode(Request r)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write((ushort)(4 + RequestSize));
        w.Write(RequestPacket);
        w.Write(Version);
        w.Write((ushort)r.Kind);
        w.Write(r.RequestId);
        w.Write(r.Epoch);
        w.Write(r.Revision);
        w.Write(r.Sequence);
        w.Write(r.Target);
        w.Write(r.Definition);
        w.Write(r.ActionId);
        Write(w, r.Position);
        return stream.ToArray();
    }
    public static ServerMessage Decode(byte[] body)
    {
        if (body.Length != EventSize)
            throw new InvalidDataException("Gameplay event length mismatch");
        using var r = new BinaryReader(new MemoryStream(body));
        if (r.ReadUInt16() != Version)
            throw new InvalidDataException("Gameplay version mismatch");
        return new ServerMessage { Kind = (EventKind)r.ReadUInt16(),
                                   Error = (ResultCode)r.ReadUInt16(),
                                   Code = r.ReadUInt16(),
                                   Entity = r.ReadInt32(),
                                   Target = r.ReadInt32(),
                                   Definition = r.ReadInt32(),
                                   Epoch = r.ReadUInt32(),
                                   Revision = r.ReadUInt32(),
                                   Sequence = r.ReadUInt32(),
                                   RequestId = r.ReadUInt64(),
                                   ActionId = r.ReadUInt64(),
                                   ServerTime = r.ReadDouble(),
                                   Position = Read(r),
                                   Velocity = Read(r),
                                   Hp = r.ReadSingle(),
                                   MaxHp = r.ReadSingle(),
                                   Radius = r.ReadSingle(),
                                   Duration = r.ReadSingle(),
                                   HitTime = r.ReadSingle(),
                                   Value = r.ReadSingle(),
                                   Mode = (PlayerMode)r.ReadUInt16(),
                                   Flags = r.ReadUInt16() };
    }
    static Vector3 Read(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    static void Write(BinaryWriter w, Vector3 p)
    {
        w.Write(p.x);
        w.Write(p.y);
        w.Write(p.z);
    }
}
}
