#include "ResourceExistsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ResourceExistsValidator::ResourceExistsValidator(WorldState &state)
    : IngameValidator(state, "ResourceExistsValidator")
{
}
Error ResourceExistsValidator::Validate(const Context &c) const
{
    return c.map.resources.count(c.request.target) ? Error::None : Error::MissingTarget;
}
} // namespace EverWind::Gameplay
