#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class CombatModeValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    CombatModeValidator();
};
}
