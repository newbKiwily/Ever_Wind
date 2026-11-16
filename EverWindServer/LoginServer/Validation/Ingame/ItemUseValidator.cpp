#include "ItemUseValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
ItemUseValidator::ItemUseValidator(MapManager &state)
    : Validator("ItemUseValidator"), world(state)
{
}

Error ItemUseValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto entry = world.Data().items.find(c.request.definition);

    if (entry == world.Data().items.end())
    {
        return Error::BadRequest;
    }

    const auto &item = entry->second;

    if (item.kind == 2)
    {
        return c.player.hp < c.player.maxHp ? Error::None : Error::Restricted;
    }

    if (item.kind == 3)
    {
        return Distance(c.player.p, c.request.position) <= 1 &&
                       Geometry(c.map, c.player.p, c.request.position, 0, c.player.id) ==
                           Error::None
                   ? Error::None
                   : Error::OutOfRange;
    }

    return Error::BadRequest;
}
}
