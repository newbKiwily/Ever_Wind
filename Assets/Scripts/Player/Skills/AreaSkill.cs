using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class AreaSkill : Skill
{
    SkillRangeDetector detector;
    bool reported;
    public override bool RequiresTarget => Definition.RequiresTarget;
    public override bool CanExecute => Definition.RangeDetector != null;

    public AreaSkill(PlayerServerTransmitter actor, ServerTransmitter target, SkillDefinition definition) : base(actor, target, definition) { }

    public override void OnStarted(ServerMessage message)
    {
        base.OnStarted(message);
        detector = Object.Instantiate(Definition.RangeDetector, Actor.transform);
        detector.transform.localPosition = Vector3.zero;
        detector.transform.localRotation = Quaternion.identity;
    }

    public override void Tick(float deltaTime)
    {
        if (reported || ActionId == 0 || Actor.Transport.EstimatedServerTime < StartTime + ImpactTime) return;
        reported = true;
        int[] targets = detector != null ? detector.Detect(Actor) : System.Array.Empty<int>();
        Actor.Send(new Request { Kind = RequestKind.AttackTargets, ActionId = ActionId, Definition = Definition.Id, Targets = targets });
        ReleaseDetector();
    }

    public override void End(bool cancelled) { ReleaseDetector(); }

    void ReleaseDetector()
    {
        if (detector != null) Object.Destroy(detector.gameObject);
        detector = null;
    }
}
}
