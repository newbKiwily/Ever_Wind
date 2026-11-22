namespace EverWind.Gameplay
{
public sealed class BuffSkill : Skill
{
    public override bool RequiresTarget => false;
    public override bool CanExecute => false;

    public BuffSkill(PlayerServerTransmitter actor, ServerTransmitter target, SkillDefinition definition) : base(actor, target, definition) { }

    // 스탯 구조 확정 후 버프 적용·지속 시간·중첩·해제를 구현한다.
    // 그 전에는 사용을 차단하고 실제 스킬 슬롯에 등록하지 않는다.
}
}
