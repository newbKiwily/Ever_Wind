#include "MapExistsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
MapExistsValidator::MapExistsValidator(WorldState &state) : IngameValidator(state, "MapExistsValidator")
{
}
Error MapExistsValidator::Validate(const Context &c) const
{
    return world.maps.count(c.request.definition) ? Error::None : Error::BadRequest;
}
} // namespace EverWind::Gameplay
