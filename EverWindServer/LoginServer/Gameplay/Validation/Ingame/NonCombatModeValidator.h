#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class NonCombatModeValidator final : public IngameValidator
{
  public:
    explicit NonCombatModeValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
