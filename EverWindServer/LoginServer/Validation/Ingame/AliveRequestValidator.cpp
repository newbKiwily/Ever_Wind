#include "AliveRequestValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
AliveRequestValidator::AliveRequestValidator()
    : Validator("AliveRequestValidator")
{
}

Error AliveRequestValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.hp <= 0 && c.request.kind != RequestKind::Revive &&
                   c.request.kind != RequestKind::ChangeMap &&
                   c.request.kind != RequestKind::Save &&
                   c.request.kind != RequestKind::Logout &&
                   c.request.kind != RequestKind::ResourceRefill
               ? Error::Dead
               : Error::None;
}
}
