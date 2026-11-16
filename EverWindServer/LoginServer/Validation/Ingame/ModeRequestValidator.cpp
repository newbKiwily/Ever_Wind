#include "ModeRequestValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ModeRequestValidator::ModeRequestValidator()
    : Validator("ModeRequestValidator")
{
}

Error ModeRequestValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.request.definition < 0 || c.request.definition > 1
               ? Error::BadRequest
           : c.player.action.id ? Error::Busy
                                : Error::None;
}
}
