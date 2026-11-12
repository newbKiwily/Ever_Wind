#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class MapExistsValidator final : public IngameValidator
{
  public:
    explicit MapExistsValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
