#include "EquipmentTypeValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
EquipmentTypeValidator::EquipmentTypeValidator(WorldState &state)
    : IngameValidator(state, "EquipmentTypeValidator")
{
}
Error EquipmentTypeValidator::Validate(const Context &c) const
{
    return world.rules.items.at(c.request.definition).kind == 1 ? Error::None : Error::BadRequest;
}
} // namespace EverWind::Gameplay
