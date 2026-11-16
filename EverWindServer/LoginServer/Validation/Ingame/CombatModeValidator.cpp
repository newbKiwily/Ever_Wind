#include "CombatModeValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
CombatModeValidator::CombatModeValidator()
    : Validator("CombatModeValidator")
{
}

Error CombatModeValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.mode == Mode::Combat ? Error::None : Error::Restricted;
}
}
