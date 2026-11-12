#include "ItemExistsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ItemExistsValidator::ItemExistsValidator(WorldState &state) : IngameValidator(state, "ItemExistsValidator")
{
}
Error ItemExistsValidator::Validate(const Context &c) const
{
    return world.rules.items.count(c.request.definition) ? Error::None : Error::BadRequest;
}
} // namespace EverWind::Gameplay
