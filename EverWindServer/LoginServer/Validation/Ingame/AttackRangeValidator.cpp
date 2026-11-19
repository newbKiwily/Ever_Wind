#include "AttackRangeValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
AttackRangeValidator::AttackRangeValidator(MapManager &state)
    : Validator("AttackRangeValidator"), world(state)
{
}

Error AttackRangeValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto target = world.Target(c.request.target, c.player.map);
    auto skill = c.player.FindSkill(c.request.definition);

    if (skill && skill->definition->kind == SkillKind::Area && !skill->definition->requiresTarget)
    {
        return Error::None;
    }

    if (!target)
    {
        return Error::MissingTarget;
    }

    if (!skill)
    {
        return Error::BadRequest;
    }

    return world.InRange(c.player, *target, skill->definition->reach)
               ? Error::None
               : Error::OutOfRange;
}
}
