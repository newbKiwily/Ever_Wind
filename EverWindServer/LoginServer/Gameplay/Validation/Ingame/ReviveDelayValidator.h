#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ReviveDelayValidator final : public IngameValidator
{
  public:
    explicit ReviveDelayValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
