#include "ResourceExistsValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ResourceExistsValidator::ResourceExistsValidator()
    : Validator("ResourceExistsValidator")
{
}

Error ResourceExistsValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.map.resources.count(c.request.target) ? Error::None
                                                   : Error::MissingTarget;
}
}
