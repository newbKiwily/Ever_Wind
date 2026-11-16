#include "AttackTargetValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
AttackTargetValidator::AttackTargetValidator(MapManager &state)
    : Validator("AttackTargetValidator"), world(state)
{
}

Error AttackTargetValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto t = world.Target(c.request.target, c.player.map);

    return !t || t->hp <= 0 || t->id == c.player.id ? Error::MissingTarget
                                                    : Error::None;
}
}
