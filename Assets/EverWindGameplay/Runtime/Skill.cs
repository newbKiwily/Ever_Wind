using System;
using UnityEngine;

namespace EverWind.Gameplay
{
public abstract class Skill
{
    protected readonly PlayerServerTransmitter Actor;
    protected readonly MonsterServerTransmitter Target;
    public SkillDefinition Definition { get; }
    public virtual bool RequiresTarget => true;
    public virtual bool CanExecute => true;
    public ulong ActionId { get; private set; }
    protected double StartTime;
    protected float ImpactTime;

    protected Skill(PlayerServerTransmitter actor, ServerTransmitter target, SkillDefinition definition)
    {
        Actor = actor;
        Target = target as MonsterServerTransmitter;
        Definition = definition;
    }

    public virtual ActionResult CheckRange(float reach)
    {
        if (!CanExecute) return ActionResult.Restricted;
        if (!RequiresTarget) return ActionResult.Running;
        if (Target == null || Target.Hp <= 0) return ActionResult.TargetLost;
        Vector3 delta = Target.transform.position - Actor.transform.position;
        delta.y = 0;
        float radius = Actor.Radius + Target.Radius + reach;
        return delta.sqrMagnitude <= radius * radius ? ActionResult.Running : ActionResult.OutOfRange;
    }

    public ulong RequestUse(Action<ServerMessage> callback)
    {
        return Actor.Send(new Request { Kind = RequestKind.Attack, Target = RequiresTarget ? Target.EntityId : 0, Definition = Definition.Id }, callback);
    }

    public virtual void OnStarted(ServerMessage message)
    {
        ActionId = message.ActionId;
        StartTime = message.Value;
        ImpactTime = message.HitTime;
    }

    public virtual void Tick(float deltaTime) { }
    public virtual void End(bool cancelled) { }
}
}
