#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class AliveRequestValidator final : public IngameValidator
{
  public:
    explicit AliveRequestValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
