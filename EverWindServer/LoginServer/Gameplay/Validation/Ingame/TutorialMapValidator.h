#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class TutorialMapValidator final : public IngameValidator
{
  public:
    explicit TutorialMapValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
