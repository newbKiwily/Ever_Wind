using UnityEngine;

namespace EverWind.Gameplay
{
public enum SkillKind : ushort { SingleTarget, Ranged, Area, Buff }

[CreateAssetMenu(menuName = "EverWind/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public int Id;
    public SkillKind Kind;
    public bool RequiresTarget = true;
    [Range(1, 5)] public int AnimationSlot = 1;
    public GameObject SwingEffect;
    public Vector3 SwingEffectOffset, SwingEffectRotation;
    public float EffectDelay = .2f;
    public SkillRangeDetector RangeDetector;
    public float Reach = 2, Damage = 10, AttackDuration = 1, Cooldown = 1;
    public float[] AttackTimes = { .5f };
    public AnimationClip Animation;
    public GameObject CastEffect;
    public Vector3 CastEffectOffset, CastEffectRotation;
    public float CastEffectTime;
    public bool AuthoredTiming;

    public virtual bool CanExecute => Kind != SkillKind.Buff;
    public bool UsesRO => Kind == SkillKind.Area || this is RangedSkill;
    public virtual bool NeedsTarget => Kind != SkillKind.Buff && (!UsesRO || RequiresTarget);

    public ActionResult CheckRange(AttackAction action, float reach)
    {
        if (!CanExecute || UsesRO && RangeDetector == null || !action.Actor.Registry.Progression.CanUseSkill(Id)) return ActionResult.Restricted;
        if (!(action.Target is MonsterServerTransmitter monster) || monster.Hp <= 0) return ActionResult.TargetLost;
        var delta = monster.transform.position - action.Actor.transform.position; delta.y = 0;
        float distance = reach + action.Actor.Radius + monster.Radius;
        return delta.sqrMagnitude <= distance * distance ? ActionResult.Running : ActionResult.OutOfRange;
    }

    public virtual Vector3 Center(Transform caster, Transform target) => caster.position;
    public virtual void Begin(AttackAction action) { }
    public virtual void Tick(AttackAction action, float dt)
    {
        if (!UsesRO) return;
        double elapsed = action.Actor.Transport.EstimatedServerTime - action.StartTime;
        float scale = AuthoredTiming ? action.Duration / AttackDuration : 1;
        float spawn = this is RangedSkill ranged ? ranged.SpawnTime * scale : 0;
        if (elapsed < spawn) return;
        if (action.RO == null)
        {
            action.RO = Instantiate(RangeDetector);
            if (this is RangedSkill definition) { action.RO.Shapes = definition.DetectionShapes; }
            action.RO.transform.SetPositionAndRotation(action.Center, action.Rotation);
            // 게임용 시각 효과는 모든 접속자의 AlivePresentation이 같은 시각에 재생한다.
            foreach (var renderer in action.RO.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var particle in action.RO.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        action.RO.transform.position = action.Center;
        int count = AuthoredTiming ? AttackTimes.Length : 1;
        while (action.NextHit < count && elapsed >= (AuthoredTiming ? AttackTimes[action.NextHit] * scale : action.LegacyImpact))
        {
            action.Actor.Send(new Request { Kind = RequestKind.AttackTargets, ActionId = action.ServerActionId, Definition = Id, Sequence = (uint)action.NextHit, Position = action.RO.transform.position, Targets = action.RO.Detect(action.Actor) });
            ++action.NextHit;
        }
    }
    public virtual void End(AttackAction action)
    {
        if (action.RO != null) Destroy(action.RO.gameObject);
        action.RO = null;
    }
}
}
