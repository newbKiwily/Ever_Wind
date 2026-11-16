#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class NonCombatModeValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    NonCombatModeValidator();
};
}
