#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ModeRequestValidator final : public IngameValidator
{
  public:
    explicit ModeRequestValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
