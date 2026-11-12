#include "CombatModeValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
CombatModeValidator::CombatModeValidator(WorldState &state) : IngameValidator(state, "CombatModeValidator")
{
}
Error CombatModeValidator::Validate(const Context &c) const
{
    return c.player.mode == Mode::Combat ? Error::None : Error::Restricted;
}
} // namespace EverWind::Gameplay
