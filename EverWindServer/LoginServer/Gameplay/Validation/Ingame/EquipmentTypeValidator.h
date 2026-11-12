#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class EquipmentTypeValidator final : public IngameValidator
{
  public:
    explicit EquipmentTypeValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
