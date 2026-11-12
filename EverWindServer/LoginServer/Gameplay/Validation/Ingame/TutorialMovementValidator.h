#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class TutorialMovementValidator final : public IngameValidator
{
  public:
    explicit TutorialMovementValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
