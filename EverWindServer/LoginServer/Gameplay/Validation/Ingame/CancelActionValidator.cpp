#include "CancelActionValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
CancelActionValidator::CancelActionValidator(WorldState &state)
    : IngameValidator(state, "CancelActionValidator")
{
}
Error CancelActionValidator::Validate(const Context &c) const
{
    return c.player.action.id && c.player.action.id == c.request.actionId ? Error::None : Error::Stale;
}
} // namespace EverWind::Gameplay
