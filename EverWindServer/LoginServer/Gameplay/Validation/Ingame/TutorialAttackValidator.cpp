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
    return world.rules.production && c.player.tutorial >= 0 && c.player.tutorial < 2 ? Error::Restricted
                                                                               : Error::None;
}
} // namespace EverWind::Gameplay
