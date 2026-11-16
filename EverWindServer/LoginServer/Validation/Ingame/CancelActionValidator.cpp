#include "CancelActionValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
CancelActionValidator::CancelActionValidator()
    : Validator("CancelActionValidator")
{
}

Error CancelActionValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.action.id && c.player.action.id == c.request.actionId
               ? Error::None
               : Error::Stale;
}
}
