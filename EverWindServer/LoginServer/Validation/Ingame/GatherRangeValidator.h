#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class GatherRangeValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    GatherRangeValidator();
};
}
