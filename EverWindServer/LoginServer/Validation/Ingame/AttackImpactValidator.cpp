#include "AttackImpactValidator.h"
namespace EverWind::Gameplay
{
AttackImpactValidator::AttackImpactValidator(MapManager &state)
    : Validator("AttackImpactValidator"), world(state)
{
}

Error AttackImpactValidator::Validate(const ValidationContext &context) const
{
    if (!context.actor || !context.target || !context.skill || !context.space)
    {
        return Error::MissingTarget;
    }

    const auto &a = *context.actor;
    const auto &t = *context.target;
    const auto &s = *context.skill;

    if (t.hp <= 0)
    {
        return Error::Dead;
    }

    if (a.hp <= 0 || t.map != a.map)
    {
        return Error::Dead;
    }

    if (s.UsesRO() && a.kind == EntityKind::Player)
    {
        Vec3 center = a.action.center;

        if (Distance(center, t.p) > s.area + (s.ro ? 0 : a.radius) + t.radius)
        {
            return Error::OutOfRange;
        }
        // 범위기는 클라이언트 탐지를 인정하고 서버는 대상별 거리만 재검사한다.
        return t.kind == EntityKind::Monster ? Error::None : Error::MissingTarget;
    }
    else
    {
        if (a.hp <= 0)
        {
            return Error::Dead;
        }

        if (!world.InRange(a, t, s.reach))
        {
            return Error::OutOfRange;
        }
    }

    return Geometry(*context.space, a.p, t.p, 0, a.id, t.id);
}
}
