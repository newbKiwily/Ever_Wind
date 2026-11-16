#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class AliveRequestValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    AliveRequestValidator();
};
}
