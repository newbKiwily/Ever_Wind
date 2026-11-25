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
        {
            Current = policies[(int)mode];
        }
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
        {
            return false;
        }
        Result = result;
        OnExit();
        return true;
    }

    protected virtual void OnExit()
    {
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
    protected virtual float GetSpeed() => Motor.MoveSpeed;
    protected virtual Vector3 GetVelocity() => GetDirection() * GetSpeed();
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
        {
            return;
        }
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
    readonly ServerTransmitter target;
    readonly float reach;
    float elapsed, stalled;
    Vector3 last;
    public ApproachTargetAction(MovementMotor motor, ServerTransmitter target, float reach) : base(motor)
    {
        this.target = target;
        this.reach = reach;
        last = motor.transform.position;
    }

    protected override Vector3 GetDirection()
    {
        if (target == null)
        {
            return Vector3.zero;
        }
        var d = target.transform.position - Motor.transform.position;
        d.y = 0;
        return d;
    }

    protected override Vector3 GetVelocity() => GetDirection().normalized * GetSpeed();
    public override void Tick(float dt)
    {
        if (Result != ActionResult.Running)
        {
            return;
        }
        if (target == null || target is MonsterServerTransmitter monster && monster.Hp <= 0)
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
        {
            Finish(ActionResult.Blocked);
        }
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
        {
            return;
        }
        float step = Mathf.Min(dt, remaining);
        // 넉백 중에는 이동 방향을 바라보지 않고 공격자를 향한 자세를 유지한다.
        Motor.Step(Direction * Speed * step, false, false);
        remaining -= step;
        if (remaining <= 0)
        {
            Finish(ActionResult.Completed);
        }
    }
}
// 공격은 이동 및 대상 접근 액션에 의존하지 않는다.
public sealed class AttackAction : PlayerAction
{
    public ulong RequestId { get; private set; }
    public SkillDefinition Skill { get; }
    public PlayerServerTransmitter Actor { get; }
    public ServerTransmitter Target { get; }
    public double StartTime { get; private set; }
    public float Duration { get; private set; }
    public float LegacyImpact { get; private set; }
    public int NextHit;
    public SkillRangeDetector RO;
    public Vector3 Center { get; private set; }
    public Quaternion Rotation { get; private set; }
    readonly float reach;
    public AttackAction(PlayerServerTransmitter actor, ServerTransmitter target, int skill, float reach)
    {
        Actor = actor; Target = target;
        Skill = actor.Registry.Catalog.Skill(skill);
        this.reach = reach;
    }

    public override void Tick(float dt)
    {
        if (Result == ActionResult.Running) Skill?.Tick(this, dt);
    }

    public void StartServerAction(ServerMessage message)
    {
        AttachServerAction(message.ActionId);
        StartTime = message.Value; Duration = message.Duration; LegacyImpact = message.HitTime; NextHit = 0;
        Rotation = Actor.transform.rotation;
        // 서버가 승인한 RO 기준점을 모든 관찰자가 공유한다.
        Center = Skill != null && Skill.UsesRO ? message.Velocity : message.Position;
        Skill?.Begin(this);
    }

    protected override void OnExit() { Release(); }
    public void Release() => Skill?.End(this);

    public ActionResult TryRequest(Action<ServerMessage> callback)
    {
        if (Skill == null) return ActionResult.Restricted;
        var check = Skill.CheckRange(this, reach);
        if (check != ActionResult.Running) return check;
        RequestId = Actor.Send(new Request { Kind = RequestKind.Attack, Target = Target.EntityId, Definition = Skill.Id, Position = Skill.Center(Actor.transform, Target.transform) }, callback);
        return ActionResult.Waiting;
    }
}

public sealed class ObtainAction : PlayerAction
{
    readonly PlayerServerTransmitter actor;
    readonly int target;
    public ObtainAction(PlayerServerTransmitter actor, int target)
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
    readonly PlayerServerTransmitter actor;
    readonly int recipe;
    public CraftAction(PlayerServerTransmitter actor, int recipe)
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
