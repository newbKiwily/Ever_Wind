#pragma once
#include "../../Gameplay/Map/RefillSystem.h"
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ResourceRefillValidator final : public Validator
{
  private:
    RefillSystem &refill;

    Error Validate(const ValidationContext &context) const override;
  public:
    ResourceRefillValidator(RefillSystem &refillSystem);
};
}
