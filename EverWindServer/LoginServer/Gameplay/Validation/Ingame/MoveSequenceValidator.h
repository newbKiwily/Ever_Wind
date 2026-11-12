#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class MoveSequenceValidator final : public IngameValidator
{
  public:
    explicit MoveSequenceValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
