#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ResourceExistsValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    ResourceExistsValidator();
};
}
