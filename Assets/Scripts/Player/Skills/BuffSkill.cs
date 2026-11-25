namespace EverWind.Gameplay
{
public sealed class BuffSkill : SkillDefinition
{
    public override bool CanExecute => false;
    void OnEnable() { Kind = SkillKind.Buff; Reach = 0; Damage = 0; RequiresTarget = false; }

    // 스탯 구조 확정 후 버프 적용·지속 시간·중첩·해제를 구현한다.
    // 그 전에는 사용을 차단하고 실제 스킬 슬롯에 등록하지 않는다.
}
}
