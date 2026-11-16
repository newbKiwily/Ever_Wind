#include "TutorialAttackValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
TutorialAttackValidator::TutorialAttackValidator(MapManager &state)
    : Validator("TutorialAttackValidator"), world(state)
{
}

Error TutorialAttackValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto step = c.player.FindTutorial(c.player.tutorial);

    return world.Data().tutorialEnabled && step && !step->definition->allowAttack
               ? Error::Restricted
               : Error::None;
}
}
