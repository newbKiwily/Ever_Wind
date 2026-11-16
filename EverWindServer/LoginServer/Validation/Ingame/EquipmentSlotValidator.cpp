#include "EquipmentSlotValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
EquipmentSlotValidator::EquipmentSlotValidator()
    : Validator("EquipmentSlotValidator")
{
}

Error EquipmentSlotValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.equipment.count(c.request.definition) ? Error::None
                                                          : Error::MissingTarget;
}
}
