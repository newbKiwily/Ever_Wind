#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class TutorialProgressValidator final : public IngameValidator
{
  public:
    explicit TutorialProgressValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
