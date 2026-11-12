#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ResourceAvailabilityValidator final : public IngameValidator
{
  public:
    explicit ResourceAvailabilityValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
