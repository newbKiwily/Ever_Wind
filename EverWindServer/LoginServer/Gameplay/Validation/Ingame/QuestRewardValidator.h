#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class QuestRewardValidator final : public IngameValidator
{
  public:
    explicit QuestRewardValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
