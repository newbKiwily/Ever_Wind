#include "GatherRangeValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
GatherRangeValidator::GatherRangeValidator(WorldState &state) : IngameValidator(state, "GatherRangeValidator")
{
}
Error GatherRangeValidator::Validate(const Context &c) const
{
    const auto &r = c.map.resources.at(c.request.target);
    return Distance(c.player.p, r.p) <= c.player.radius + r.radius + 1.2f ? Error::None : Error::OutOfRange;
}
} // namespace EverWind::Gameplay
