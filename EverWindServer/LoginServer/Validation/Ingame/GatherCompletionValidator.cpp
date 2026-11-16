#include "GatherCompletionValidator.h"
namespace EverWind::Gameplay
{
GatherCompletionValidator::GatherCompletionValidator()
    : Validator("GatherCompletionValidator")
{
}

Error GatherCompletionValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto it = c.map.resources.find(c.request.target);

    if (it == c.map.resources.end())
    {
        return Error::MissingTarget;
    }

    return it->second->owner == c.player.id && it->second->stock > 0
               ? Error::None
               : Error::Cancelled;
}
}
