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
    auto found = world.rules.tutorials.find(phase);
    if (found == world.rules.tutorials.end()) return Error::Stale;
    const auto &step = found->second;
    if (c.now - c.player.tutorialStart < step.minimumSeconds) return Error::Restricted;
    return step.eventKind == -1 || (c.player.tutorialEvidence & (1u << phase)) ? Error::None : Error::Restricted;
}
} // namespace EverWind::Gameplay
