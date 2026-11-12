#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class CancelActionValidator final : public IngameValidator
{
  public:
    explicit CancelActionValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
