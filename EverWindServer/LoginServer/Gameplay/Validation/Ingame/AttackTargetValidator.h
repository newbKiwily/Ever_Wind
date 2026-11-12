#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class AttackTargetValidator final : public IngameValidator
{
  public:
    explicit AttackTargetValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
