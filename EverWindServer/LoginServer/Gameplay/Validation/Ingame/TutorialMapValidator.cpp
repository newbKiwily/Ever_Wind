#include "TutorialMapValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
TutorialMapValidator::TutorialMapValidator(WorldState &state) : IngameValidator(state, "TutorialMapValidator")
{
}
Error TutorialMapValidator::Validate(const Context &c) const
{
    return world.rules.production && c.request.definition == 1 && c.player.tutorial >= 0 ? Error::Restricted
                                                                                   : Error::None;
}
} // namespace EverWind::Gameplay
