#include "ReviveDelayValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ReviveDelayValidator::ReviveDelayValidator(WorldState &state) : IngameValidator(state, "ReviveDelayValidator")
{
}
Error ReviveDelayValidator::Validate(const Context &c) const
{
    return c.now < c.player.reviveAt ? Error::Cooldown : Error::None;
}
} // namespace EverWind::Gameplay
