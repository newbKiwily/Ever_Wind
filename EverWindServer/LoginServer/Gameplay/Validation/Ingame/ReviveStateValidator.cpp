#include "ReviveStateValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ReviveStateValidator::ReviveStateValidator(WorldState &state) : IngameValidator(state, "ReviveStateValidator")
{
}
Error ReviveStateValidator::Validate(const Context &c) const
{
    return c.player.hp <= 0 ? Error::None : Error::Restricted;
}
} // namespace EverWind::Gameplay
