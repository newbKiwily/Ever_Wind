#include "ResourceRefillValidator.h"
#include "../../Gameplay/Map/RefillSystem.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ResourceRefillValidator::ResourceRefillValidator(RefillSystem &refillSystem)
    : Validator("ResourceRefillValidator"), refill(refillSystem)
{
}

Error ResourceRefillValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return refill.ValidateResource(c);
}
}
