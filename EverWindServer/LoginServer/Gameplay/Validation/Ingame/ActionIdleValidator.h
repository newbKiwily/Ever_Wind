#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ActionIdleValidator final : public IngameValidator
{
  public:
    explicit ActionIdleValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
