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
    auto step = world.rules.tutorials.find(c.player.tutorial);
    return world.rules.tutorialEnabled && step != world.rules.tutorials.end() && !step->second.allowMove ? Error::Restricted : Error::None;
}
} // namespace EverWind::Gameplay
