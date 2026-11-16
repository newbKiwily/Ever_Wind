#include "ForcedMovementValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ForcedMovementValidator::ForcedMovementValidator()
    : Validator("ForcedMovementValidator")
{
}

Error ForcedMovementValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.pushEnd > c.now && c.request.kind != RequestKind::Cancel &&
                   c.request.kind != RequestKind::Save &&
                   c.request.kind != RequestKind::Logout &&
                   c.request.kind != RequestKind::ResourceRefill
               ? Error::Restricted
               : Error::None;
}
}
