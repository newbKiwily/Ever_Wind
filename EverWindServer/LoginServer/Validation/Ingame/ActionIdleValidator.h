#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ActionIdleValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    ActionIdleValidator();
};
}
