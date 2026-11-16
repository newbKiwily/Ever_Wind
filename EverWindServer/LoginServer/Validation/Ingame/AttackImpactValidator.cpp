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

    if (s.area > 0 && a.kind == EntityKind::Player)
    {
        if (Distance(a.p, t.p) > s.area + t.radius)
        {
            return Error::OutOfRange;
        }
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
