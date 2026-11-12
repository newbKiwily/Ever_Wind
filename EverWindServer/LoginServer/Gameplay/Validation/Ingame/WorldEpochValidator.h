#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class WorldEpochValidator final : public IngameValidator
{
  public:
    explicit WorldEpochValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
