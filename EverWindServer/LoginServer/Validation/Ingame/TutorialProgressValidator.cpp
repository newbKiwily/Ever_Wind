#include "TutorialProgressValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
TutorialProgressValidator::TutorialProgressValidator()
    : Validator("TutorialProgressValidator")
{
}

Error TutorialProgressValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    int stepId = c.player.tutorial;

    if (stepId < 0 || stepId != c.request.definition)
    {
        return Error::Stale;
    }

    auto found = c.player.FindTutorial(stepId);

    if (!found)
    {
        return Error::Stale;
    }

    const auto &step = *found->definition;

    if (c.now - c.player.TutorialStart() < step.minimumSeconds)
    {
        return Error::Restricted;
    }

    return step.eventKind == -1 || (c.player.TutorialEvidence() & (1u << stepId))
               ? Error::None
               : Error::Restricted;
}
}
