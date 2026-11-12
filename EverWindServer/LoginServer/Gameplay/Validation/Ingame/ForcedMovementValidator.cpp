#include "ForcedMovementValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ForcedMovementValidator::ForcedMovementValidator(WorldState &state)
    : IngameValidator(state, "ForcedMovementValidator")
{
}
Error ForcedMovementValidator::Validate(const Context &c) const
{
    return c.player.pushEnd > c.now && c.request.kind != RequestKind::Cancel &&
                   c.request.kind != RequestKind::Save && c.request.kind != RequestKind::Logout &&
                   c.request.kind != RequestKind::ResourceRefill
               ? Error::Restricted
               : Error::None;
}
} // namespace EverWind::Gameplay
