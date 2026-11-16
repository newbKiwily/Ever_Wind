#include "ActionIdleValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ActionIdleValidator::ActionIdleValidator()
    : Validator("ActionIdleValidator")
{
}

Error ActionIdleValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.action.id ? Error::Busy : Error::None;
}
}
