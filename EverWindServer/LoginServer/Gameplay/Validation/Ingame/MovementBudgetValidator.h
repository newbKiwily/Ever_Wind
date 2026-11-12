#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class MovementBudgetValidator final : public IngameValidator
{
  public:
    explicit MovementBudgetValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
