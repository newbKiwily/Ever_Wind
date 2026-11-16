#include "TutorialMovementValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
TutorialMovementValidator::TutorialMovementValidator(MapManager &state)
    : Validator("TutorialMovementValidator"), world(state)
{
}

Error TutorialMovementValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto step = c.player.FindTutorial(c.player.tutorial);

    return world.Data().tutorialEnabled && step && !step->definition->allowMove
               ? Error::Restricted
               : Error::None;
}
}
