#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ResourceAvailabilityValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    ResourceAvailabilityValidator();
};
}
