using UnityEngine;

namespace EverWind.Gameplay
{
public enum SkillKind : ushort { SingleTarget, Ranged, Area, Buff }

[CreateAssetMenu(menuName = "EverWind/Skill Definition")]
public sealed class SkillDefinition : ScriptableObject
{
    public int Id;
    public SkillKind Kind;
    public bool RequiresTarget = true;
    [Range(1, 5)] public int AnimationSlot = 1;
    public GameObject SwingEffect;
    public GameObject HitEffect;
    public float EffectDelay = .2f;
    public SkillRangeDetector RangeDetector;

    public Skill Create(PlayerServerTransmitter actor, ServerTransmitter target)
    {
        switch (Kind)
        {
        case SkillKind.SingleTarget: return new SingleTargetSkill(actor, target, this);
        case SkillKind.Ranged: return new RangedSkill(actor, target, this);
        case SkillKind.Area: return new AreaSkill(actor, target, this);
        case SkillKind.Buff: return new BuffSkill(actor, target, this);
        default: throw new System.ArgumentOutOfRangeException(nameof(Kind));
        }
    }
}
}
