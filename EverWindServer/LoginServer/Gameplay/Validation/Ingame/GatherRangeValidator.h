#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class GatherRangeValidator final : public IngameValidator
{
  public:
    explicit GatherRangeValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
