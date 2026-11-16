#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ForcedMovementValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    ForcedMovementValidator();
};
}
