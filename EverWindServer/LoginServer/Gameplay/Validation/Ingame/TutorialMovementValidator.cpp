#include "TutorialMovementValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
TutorialMovementValidator::TutorialMovementValidator(WorldState &state)
    : IngameValidator(state, "TutorialMovementValidator")
{
}
Error TutorialMovementValidator::Validate(const Context &c) const
{
    return world.rules.production && c.player.tutorial == 0 ? Error::Restricted : Error::None;
}
} // namespace EverWind::Gameplay
