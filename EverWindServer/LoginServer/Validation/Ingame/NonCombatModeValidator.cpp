#include "NonCombatModeValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
NonCombatModeValidator::NonCombatModeValidator()
    : Validator("NonCombatModeValidator")
{
}

Error NonCombatModeValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.mode == Mode::NonCombat ? Error::None : Error::Restricted;
}
}
