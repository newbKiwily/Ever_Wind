#include "MapExistsValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
MapExistsValidator::MapExistsValidator(MapManager &state)
    : Validator("MapExistsValidator"), world(state)
{
}

Error MapExistsValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return world.HasMap(c.request.definition) ? Error::None : Error::BadRequest;
}
}
