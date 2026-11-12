#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class EquipmentSlotValidator final : public IngameValidator
{
  public:
    explicit EquipmentSlotValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
