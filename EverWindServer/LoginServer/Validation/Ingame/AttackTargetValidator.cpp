#include "AttackTargetValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
AttackTargetValidator::AttackTargetValidator(MapManager &state)
    : Validator("AttackTargetValidator"), world(state)
{
}

Error AttackTargetValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto skill = c.player.FindSkill(c.request.definition);

    if (!skill || skill->definition->kind == SkillKind::Buff)
    {
        return Error::Restricted;
    }

    auto t = world.Target(c.request.target, c.player.map);

    return !t || t->hp <= 0 || t->kind != EntityKind::Monster ? Error::MissingTarget
                                                              : Error::None;
}
}
