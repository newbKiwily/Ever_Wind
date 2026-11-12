#include "NonCombatModeValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
NonCombatModeValidator::NonCombatModeValidator(WorldState &state)
    : IngameValidator(state, "NonCombatModeValidator")
{
}
Error NonCombatModeValidator::Validate(const Context &c) const
{
    return c.player.mode == Mode::NonCombat ? Error::None : Error::Restricted;
}
} // namespace EverWind::Gameplay
