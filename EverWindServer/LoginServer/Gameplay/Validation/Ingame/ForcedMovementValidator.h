#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ForcedMovementValidator final : public IngameValidator
{
  public:
    explicit ForcedMovementValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
