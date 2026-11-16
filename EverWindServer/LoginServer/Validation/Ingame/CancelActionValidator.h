#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class CancelActionValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    CancelActionValidator();
};
}
