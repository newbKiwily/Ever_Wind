#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class CombatModeValidator final : public IngameValidator
{
  public:
    explicit CombatModeValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
