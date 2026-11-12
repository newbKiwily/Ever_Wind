#include "TutorialProgressValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
TutorialProgressValidator::TutorialProgressValidator(WorldState &state)
    : IngameValidator(state, "TutorialProgressValidator")
{
}
Error TutorialProgressValidator::Validate(const Context &c) const
{
    int phase = c.player.tutorial;
    if (phase < 0 || phase != c.request.definition)
        return Error::Stale;
    return phase == 0 ? (c.now - c.player.tutorialStart >= 5 ? Error::None : Error::Restricted)
           : (c.player.tutorialEvidence & (1u << phase)) ? Error::None
                                                         : Error::Restricted;
}
} // namespace EverWind::Gameplay
