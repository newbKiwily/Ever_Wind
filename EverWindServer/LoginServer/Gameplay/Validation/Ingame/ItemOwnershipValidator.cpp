#include "ItemOwnershipValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ItemOwnershipValidator::ItemOwnershipValidator(WorldState &state)
    : IngameValidator(state, "ItemOwnershipValidator")
{
}
Error ItemOwnershipValidator::Validate(const Context &c) const
{
    auto i = c.player.inventory.find(c.request.definition);
    return i != c.player.inventory.end() && i->second > 0 ? Error::None : Error::Ingredients;
}
} // namespace EverWind::Gameplay
