#include "EquipmentSlotValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
EquipmentSlotValidator::EquipmentSlotValidator(WorldState &state)
    : IngameValidator(state, "EquipmentSlotValidator")
{
}
Error EquipmentSlotValidator::Validate(const Context &c) const
{
    return c.player.equipment.count(c.request.definition) ? Error::None : Error::MissingTarget;
}
} // namespace EverWind::Gameplay
