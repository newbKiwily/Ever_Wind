#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class EquipmentSlotValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    EquipmentSlotValidator();
};
}
