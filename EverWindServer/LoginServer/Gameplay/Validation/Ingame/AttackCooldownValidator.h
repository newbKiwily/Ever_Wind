#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class AttackCooldownValidator final : public IngameValidator
{
  public:
    explicit AttackCooldownValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
