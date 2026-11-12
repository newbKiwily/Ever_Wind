#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class AttackRangeValidator final : public IngameValidator
{
  public:
    explicit AttackRangeValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
