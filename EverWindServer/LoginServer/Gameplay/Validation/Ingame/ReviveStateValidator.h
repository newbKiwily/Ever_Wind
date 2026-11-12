#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ReviveStateValidator final : public IngameValidator
{
  public:
    explicit ReviveStateValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
