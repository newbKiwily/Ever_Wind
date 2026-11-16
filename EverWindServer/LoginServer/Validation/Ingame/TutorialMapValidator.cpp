#include "TutorialMapValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
TutorialMapValidator::TutorialMapValidator(MapManager &state)
    : Validator("TutorialMapValidator"), world(state)
{
}

Error TutorialMapValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return world.Data().tutorialEnabled &&
                   c.request.definition == world.Data().tutorialExitMapId &&
                   c.player.tutorial >= 0
               ? Error::Restricted
               : Error::None;
}
}
