#include "AliveRequestValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
AliveRequestValidator::AliveRequestValidator(WorldState &state)
    : IngameValidator(state, "AliveRequestValidator")
{
}
Error AliveRequestValidator::Validate(const Context &c) const
{
    return c.player.hp <= 0 && c.request.kind != RequestKind::Revive &&
                   c.request.kind != RequestKind::ChangeMap && c.request.kind != RequestKind::Save &&
                   c.request.kind != RequestKind::Logout && c.request.kind != RequestKind::ResourceRefill
               ? Error::Dead
               : Error::None;
}
} // namespace EverWind::Gameplay
