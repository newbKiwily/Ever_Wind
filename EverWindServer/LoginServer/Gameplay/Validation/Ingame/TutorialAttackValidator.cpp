#include "TutorialAttackValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
TutorialAttackValidator::TutorialAttackValidator(WorldState &state)
    : IngameValidator(state, "TutorialAttackValidator")
{
}
Error TutorialAttackValidator::Validate(const Context &c) const
{
    auto step = world.rules.tutorials.find(c.player.tutorial);
    return world.rules.tutorialEnabled && step != world.rules.tutorials.end() && !step->second.allowAttack ? Error::Restricted : Error::None;
}
} // namespace EverWind::Gameplay
