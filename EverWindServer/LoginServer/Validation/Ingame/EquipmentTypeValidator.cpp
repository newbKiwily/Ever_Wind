#include "EquipmentTypeValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
EquipmentTypeValidator::EquipmentTypeValidator(MapManager &state)
    : Validator("EquipmentTypeValidator"), world(state)
{
}

Error EquipmentTypeValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto item = world.Data().items.find(c.request.definition);

    if (item == world.Data().items.end())
    {
        return Error::BadRequest;
    }

    return item->second.kind == 1 ? Error::None : Error::BadRequest;
}
}
