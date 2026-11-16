#include "ReviveDelayValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ReviveDelayValidator::ReviveDelayValidator()
    : Validator("ReviveDelayValidator")
{
}

Error ReviveDelayValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.now < c.player.reviveAt ? Error::Cooldown : Error::None;
}
}
