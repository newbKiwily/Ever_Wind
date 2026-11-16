#include "WorldEpochValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
WorldEpochValidator::WorldEpochValidator()
    : Validator("WorldEpochValidator")
{
}

Error WorldEpochValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.epoch != c.request.epoch ? Error::WrongWorld : Error::None;
}
}
