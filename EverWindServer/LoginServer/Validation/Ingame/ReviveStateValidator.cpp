#include "ReviveStateValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ReviveStateValidator::ReviveStateValidator()
    : Validator("ReviveStateValidator")
{
}

Error ReviveStateValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.hp <= 0 ? Error::None : Error::Restricted;
}
}
