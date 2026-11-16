#include "AttackCooldownValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
AttackCooldownValidator::AttackCooldownValidator()
    : Validator("AttackCooldownValidator")
{
}

Error AttackCooldownValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto skill = c.player.FindSkill(c.request.definition);

    return !skill                         ? Error::BadRequest
           : skill->cooldownUntil > c.now ? Error::Cooldown
                                          : Error::None;
}
}
