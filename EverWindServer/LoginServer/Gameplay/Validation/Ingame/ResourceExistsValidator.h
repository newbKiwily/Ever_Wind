#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ResourceExistsValidator final : public IngameValidator
{
  public:
    explicit ResourceExistsValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
