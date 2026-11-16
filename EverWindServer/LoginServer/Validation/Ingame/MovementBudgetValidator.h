#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class MovementBudgetValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    MovementBudgetValidator();
};
}
