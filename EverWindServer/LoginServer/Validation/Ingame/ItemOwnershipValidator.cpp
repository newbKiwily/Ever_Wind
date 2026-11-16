#include "ItemOwnershipValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ItemOwnershipValidator::ItemOwnershipValidator()
    : Validator("ItemOwnershipValidator")
{
}

Error ItemOwnershipValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto i = c.player.inventory.find(c.request.definition);

    return i != c.player.inventory.end() && i->second > 0 ? Error::None
                                                          : Error::Ingredients;
}
}
