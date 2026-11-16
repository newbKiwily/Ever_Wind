#include "MovementGeometryValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
MovementGeometryValidator::MovementGeometryValidator()
    : Validator("MovementGeometryValidator")
{
}

Error MovementGeometryValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return Geometry(c.map, c.player.p, c.request.position, c.player.radius, c.player.id);
}
}
