using System;
using System.Collections.Generic;

using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class PlayerServerTransmitter : AliveServerTransmitter
{
    private uint sequence, revision, confirmed;

    private readonly Dictionary<uint, Vector3> predicted = new Dictionary<uint, Vector3>();

    public PlayerStateContext State { get; } = new PlayerStateContext();
    public PlayerActionController Controller;

    public override bool IsLocal => Transport != null && Transport.LocalEntityId == EntityId;

    public ulong UpgradeStat(StatKind stat, Action<ServerMessage> callback = null) => Send(new Request { Kind = RequestKind.UpgradeStat, Definition = (int)stat }, callback);
    public ulong UpgradeSkill(int skill, Action<ServerMessage> callback = null) => Send(new Request { Kind = RequestKind.UpgradeSkill, Definition = skill }, callback);
    public ulong ResetStats(Action<ServerMessage> callback = null) => Send(new Request { Kind = RequestKind.ResetStats }, callback);
    public ulong ResetSkills(Action<ServerMessage> callback = null) => Send(new Request { Kind = RequestKind.ResetSkills }, callback);

    public override void Receive(ServerMessage message)
    {
        if (message.Kind == EventKind.Stats)
        {
            var motor = GetComponent<MovementMotor>();

            if (motor != null)
            {
                motor.MoveSpeed = message.Value;
            }
        }

        base.Receive(message);
        Controller?.Handle(message);
    }

    public Request CreateMoveRequest(Vector3 position)
    {
        uint next = ++sequence;

        predicted[next] = position;

        if (predicted.Count > 128)
        {
            predicted.Clear();

            Transport.CloseConnection();

            return null;
        }

        return new Request { Kind = RequestKind.Move, Position = position, Sequence = next, Revision = revision };
    }

    protected override void ApplyServerPosition(ServerMessage message)
    {
        var motor = GetComponent<MovementMotor>();

        if (message.Kind == EventKind.Hit && (message.Flags & (ushort)HitReaction.Knockback) != 0)
        {
            revision = message.Revision;

            predicted.Clear();

            motor?.ClearPending();

            return;
        }

        if (message.Kind == EventKind.State)
        {
            if (message.Revision > revision || message.Mode == PlayerMode.Dead || (message.Flags & (ushort)HitReaction.Knockback) != 0 || motor != null && motor.Forced)
            {
                ResetPosition(message, motor);
            }

            return;
        }

        if (message.Kind != EventKind.MoveResult || message.Revision < revision)
        {
            return;
        }

        if (message.Error != ResultCode.None || message.Revision > revision)
        {
            ResetPosition(message, motor);

            confirmed = message.Sequence;

            return;
        }

        if (message.Sequence <= confirmed)
        {
            return;
        }

        confirmed = message.Sequence;

        sequence = sequence > message.Sequence ? sequence : message.Sequence;

        if (predicted.TryGetValue(message.Sequence, out var position))
        {
            Vector3 difference = message.Position - position;

            if (difference.sqrMagnitude > .0001f)
            {
                ApplyPosition(transform.position + difference);
            }
        }

        var completed = new List<uint>();

        foreach (uint key in predicted.Keys)
        {
            if (key <= confirmed)
            {
                completed.Add(key);
            }
        }

        foreach (uint key in completed)
        {
            predicted.Remove(key);
        }
    }

    private void ResetPosition(ServerMessage message, MovementMotor motor)
    {
        revision = message.Revision;

        sequence = sequence > message.Sequence ? sequence : message.Sequence;

        predicted.Clear();

        motor?.ClearPending();

        ApplyPosition(message.Position);
    }

    protected override void ApplyMode(PlayerMode mode)
    {
        base.ApplyMode(mode);

        State.Apply(mode);
    }

    protected override bool CanSend(Request request)
    {
        return IsLocal && Transport.Ready;
    }
}
}
