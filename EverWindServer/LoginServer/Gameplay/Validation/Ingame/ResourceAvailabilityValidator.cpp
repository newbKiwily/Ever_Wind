#include "ResourceAvailabilityValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ResourceAvailabilityValidator::ResourceAvailabilityValidator(WorldState &state)
    : IngameValidator(state, "ResourceAvailabilityValidator")
{
}
Error ResourceAvailabilityValidator::Validate(const Context &c) const
{
    const auto &r = c.map.resources.at(c.request.target);
    return r.stock <= 0 ? Error::Exhausted : r.owner ? Error::Busy : Error::None;
}
} // namespace EverWind::Gameplay
