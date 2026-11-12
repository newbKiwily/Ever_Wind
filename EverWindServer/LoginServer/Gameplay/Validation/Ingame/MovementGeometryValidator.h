#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class MovementGeometryValidator final : public IngameValidator
{
  public:
    explicit MovementGeometryValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
