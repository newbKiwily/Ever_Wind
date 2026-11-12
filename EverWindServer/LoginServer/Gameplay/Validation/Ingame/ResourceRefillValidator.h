#pragma once
#include "../IngameValidator.h"
#include "../../Refill/RefillSystem.h"
namespace EverWind::Gameplay
{
class ResourceRefillValidator final : public IngameValidator
{
  public:
    explicit ResourceRefillValidator(WorldState &state, RefillSystem &refillSystem);

  private:
    Error Validate(const Context &c) const override;
    RefillSystem &refill;
};
} // namespace EverWind::Gameplay
