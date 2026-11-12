#include "ActionIdleValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ActionIdleValidator::ActionIdleValidator(WorldState &state) : IngameValidator(state, "ActionIdleValidator")
{
}
Error ActionIdleValidator::Validate(const Context &c) const
{
    return c.player.action.id ? Error::Busy : Error::None;
}
} // namespace EverWind::Gameplay
