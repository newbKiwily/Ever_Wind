#include "MoveSequenceValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
MoveSequenceValidator::MoveSequenceValidator()
    : Validator("MoveSequenceValidator")
{
}

Error MoveSequenceValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.request.revision != c.player.revision ||
                   c.request.sequence <= c.player.sequence
               ? Error::Stale
               : Error::None;
}
}
