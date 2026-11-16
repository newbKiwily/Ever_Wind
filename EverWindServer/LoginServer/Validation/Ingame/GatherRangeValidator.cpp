#include "GatherRangeValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
GatherRangeValidator::GatherRangeValidator()
    : Validator("GatherRangeValidator")
{
}

Error GatherRangeValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto resource = c.map.resources.find(c.request.target);

    if (resource == c.map.resources.end())
    {
        return Error::MissingTarget;
    }

    const auto &r = *resource->second;

    return Distance(c.player.p, r.p) <= c.player.radius + r.radius + 1.2f
               ? Error::None
               : Error::OutOfRange;
}
}
