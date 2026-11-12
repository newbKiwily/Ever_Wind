#include "MovementGeometryValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
MovementGeometryValidator::MovementGeometryValidator(WorldState &state)
    : IngameValidator(state, "MovementGeometryValidator")
{
}
Error MovementGeometryValidator::Validate(const Context &c) const
{
    return Geometry(c.map, c.player.p, c.request.position, c.player.radius);
}
} // namespace EverWind::Gameplay
