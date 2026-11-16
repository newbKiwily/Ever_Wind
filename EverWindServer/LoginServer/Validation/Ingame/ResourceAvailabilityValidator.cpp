#include "ResourceAvailabilityValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ResourceAvailabilityValidator::ResourceAvailabilityValidator()
    : Validator("ResourceAvailabilityValidator")
{
}

Error ResourceAvailabilityValidator::Validate(const ValidationContext &context) const
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

    return r.stock <= 0 ? Error::Exhausted : r.owner ? Error::Busy
                                                     : Error::None;
}
}
