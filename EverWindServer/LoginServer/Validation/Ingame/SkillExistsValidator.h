#pragma once
#include "../Validator.h"
namespace EverWind::Gameplay
{
class SkillExistsValidator final : public Validator
{
  private:
    Error Validate(const ValidationContext &context) const override;
  public:
    SkillExistsValidator();
};
}
