#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class TutorialAttackValidator final : public IngameValidator
{
  public:
    explicit TutorialAttackValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
