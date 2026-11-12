using System;
using UnityEngine;

namespace EverWind.Gameplay
{
public enum ActionResult
{
    Running,
    Completed,
    ReachedTarget,
    Cancelled,
    TargetLost,
    Blocked,
    TimedOut,
    OutOfRange,
    Restricted,
    Waiting
}
public interface IModePolicy
{
    PlayerMode Mode { get; }
    bool Allows(ActionKind kind);
    string Locomotion(bool moving);
}
public sealed class NonCombatState : IModePolicy
{
    public PlayerMode Mode => PlayerMode.NonCombat;
    public bool Allows(ActionKind k) => k == ActionKind.None || k == ActionKind.Gather
                                        || k == ActionKind.Craft;
    public string Locomotion(bool moving) => moving ? "Run" : "Idle";
}
public sealed class CombatState : IModePolicy
{
    public PlayerMode Mode => PlayerMode.Combat;
    public bool Allows(ActionKind k) => k == ActionKind.None || k == ActionKind.Attack;
    public string Locomotion(bool moving) => moving ? "CombatRun" : "CombatIdle";
}
public sealed class LootingState : IModePolicy
{
    public PlayerMode Mode => PlayerMode.Looting;
    public bool Allows(ActionKind k) => false;
    public string Locomotion(bool moving) => "Gather";
}
public sealed class DeadState : IModePolicy
{
    public PlayerMode Mode => PlayerMode.Dead;
    public bool Allows(ActionKind k) => false;
    public string Locomotion(bool moving) => "Dead";
}
public sealed class PlayerStateContext
{
    readonly IModePolicy[] policies = { new NonCombatState(), new CombatState(), new LootingState(),
                                        new DeadState() };
    public IModePolicy Current { get; private set; }
    public PlayerStateContext()
    {
        Current = policies[0];
    }
    public void Apply(PlayerMode mode)
    {
        if ((int)mode < policies.Length)
            Current = policies[(int)mode];
    }
}
public abstract class PlayerAction
{
    public ulong ExecutionId { get; private set; }
    public ulong ServerActionId { get; private set; }
    public ActionResult Result { get; private set; } = ActionResult.Cancelled;
    public virtual void Begin(ulong id)
    {
        ExecutionId = id;
        ServerActionId = 0;
        Result = ActionResult.Running;
    }
    public void AttachServerAction(ulong id)
    {
        ServerActionId = id;
    }
    public abstract void Tick(float dt);
    public bool Finish(ActionResult result)
    {
        if (Result != ActionResult.Running)
            return false;
        Result = result;
        OnExit();
        return true;
    }
    protected virtual void OnExit()
    {
    }
}
// Cosmetic overlay; a normal hit does not take movement ownership or cancel a server action.
public sealed class HitAction : PlayerAction
{
    float elapsed;
    public float Duration = .25f;
    public override void Begin(ulong id)
    {
        base.Begin(id);
        elapsed = 0;
    }
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
            return;
        elapsed += dt;
        if (elapsed >= Duration)
            Finish(ActionResult.Completed);
    }
}
public class MoveAction : PlayerAction
{
    protected readonly MovementMotor Motor;
    public Vector3 Direction;
    public float Speed = 5;
    public MoveAction(MovementMotor motor)
    {
        Motor = motor;
        Speed = motor.MoveSpeed;
    }
    protected virtual Vector3 GetDirection() => Direction;
    protected virtual float GetSpeed() => Speed;
    protected virtual Vector3 GetVelocity() => GetDirection() * GetSpeed();
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
            return;
        var d = GetDirection();
        if (d.sqrMagnitude < .0001f)
        {
            Finish(ActionResult.Completed);
            return;
        }
        Motor.Step(GetVelocity() * dt);
    }
}
public sealed class ApproachTargetAction : MoveAction
{
    readonly ServerTransceiver target;
    readonly float reach;
    float elapsed, stalled;
    Vector3 last;
    public ApproachTargetAction(MovementMotor motor, ServerTransceiver target, float reach) : base(motor)
    {
        this.target = target;
        this.reach = reach;
        last = motor.transform.position;
    }
    protected override Vector3 GetDirection()
    {
        if (target == null)
            return Vector3.zero;
        var d = target.transform.position - Motor.transform.position;
        d.y = 0;
        return d;
    }
    protected override Vector3 GetVelocity() => GetDirection().normalized * GetSpeed();
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
            return;
        if (target == null || target.Hp <= 0 && target.Kind != EntityKind.Resource)
        {
            Finish(ActionResult.TargetLost);
            return;
        }
        var d = GetDirection();
        float distance = Motor.Radius + target.Radius + reach;
        if (d.sqrMagnitude <= distance * distance)
        {
            Finish(ActionResult.ReachedTarget);
            return;
        }
        elapsed += dt;
        if (elapsed > 15)
        {
            Finish(ActionResult.TimedOut);
            return;
        }
        base.Tick(dt);
        stalled = (Motor.transform.position - last).sqrMagnitude < .000001f ? stalled + dt : 0;
        last = Motor.transform.position;
        if (stalled > 1.5f)
            Finish(ActionResult.Blocked);
    }
}
public sealed class KnockbackAction : MoveAction
{
    float remaining;
    public KnockbackAction(MovementMotor motor, Vector3 velocity, float duration) : base(motor)
    {
        Speed = velocity.magnitude;
        Direction = velocity.normalized;
        remaining = duration;
    }
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
            return;
        float step = Mathf.Min(dt, remaining);
        Motor.Step(Direction * Speed * step, false);
        remaining -= step;
        if (remaining <= 0)
            Finish(ActionResult.Completed);
    }
}
// Deliberately no dependency on MoveAction/ApproachTargetAction.
public sealed class AttackAction : PlayerAction
{
    public ulong RequestId { get; private set; }
    readonly ServerTransceiver actor, target;
    readonly int skill;
    readonly float reach;
    public AttackAction(ServerTransceiver actor, ServerTransceiver target, int skill, float reach)
    {
        this.actor = actor;
        this.target = target;
        this.skill = skill;
        this.reach = reach;
    }
    public override void Tick(float dt)
    {
    }
    public ActionResult TryRequest(Action<ServerMessage> callback)
    {
        if (target == null || target.Hp <= 0)
            return ActionResult.TargetLost;
        var d = target.transform.position - actor.transform.position;
        d.y = 0;
        float r = actor.Radius + target.Radius + reach;
        if (d.sqrMagnitude > r * r)
            return ActionResult.OutOfRange;
        RequestId = actor.Send(
            new Request { Kind = RequestKind.Attack, Target = target.EntityId, Definition = skill },
            callback);
        return ActionResult.Waiting;
    }
}
public sealed class ObtainAction : PlayerAction
{
    readonly ServerTransceiver actor;
    readonly int target;
    public ObtainAction(ServerTransceiver actor, int target)
    {
        this.actor = actor;
        this.target = target;
    }
    public ulong Request(Action<ServerMessage> callback) =>
        actor.Send(new Request { Kind = RequestKind.Gather, Target = target }, callback);
    public override void Tick(float dt)
    {
    }
}
public sealed class CraftAction : PlayerAction
{
    readonly ServerTransceiver actor;
    readonly int recipe;
    public CraftAction(ServerTransceiver actor, int recipe)
    {
        this.actor = actor;
        this.recipe = recipe;
    }
    public ulong Request(Action<ServerMessage> callback) => actor.Send(new Request { Kind = RequestKind.Craft,
                                                                                     Definition = recipe },
                                                                       callback);
    public override void Tick(float dt)
    {
    }
}
}
