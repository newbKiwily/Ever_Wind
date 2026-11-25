namespace EverWind.Gameplay
{
public sealed class SingleTargetSkill : SkillDefinition
{
    void OnEnable() { Kind = SkillKind.SingleTarget; RequiresTarget = true; }
}
}
