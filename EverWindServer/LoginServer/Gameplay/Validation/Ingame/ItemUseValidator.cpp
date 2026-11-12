#include "ItemUseValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ItemUseValidator::ItemUseValidator(WorldState &state) : IngameValidator(state, "ItemUseValidator")
{
}
Error ItemUseValidator::Validate(const Context &c) const
{
    const auto &item = world.rules.items.at(c.request.definition);
    if (item.kind == 2)
        return c.player.hp < c.player.maxHp ? Error::None : Error::Restricted;
    if (item.kind == 3)
        return Distance(c.player.p, c.request.position) <= 1 &&
                       Geometry(c.map, c.player.p, c.request.position, 0, true) == Error::None
                   ? Error::None
                   : Error::OutOfRange;
    return Error::BadRequest;
}
} // namespace EverWind::Gameplay
