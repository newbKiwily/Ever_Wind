namespace EverWind.Gameplay
{
// 원거리도 즉발 명중만 사용하며 투사체를 생성하지 않는다.
public sealed class RangedSkill : SingleTargetSkill
{
    public RangedSkill(PlayerServerTransmitter actor, ServerTransmitter target, SkillDefinition definition) : base(actor, target, definition) { }
}
}
