namespace EverWind.Gameplay
{
public class SingleTargetSkill : Skill
{
    public SingleTargetSkill(PlayerServerTransmitter actor, ServerTransmitter target, SkillDefinition definition) : base(actor, target, definition) { }
}
}
