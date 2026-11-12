#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class CraftIngredientsValidator final : public IngameValidator
{
  public:
    explicit CraftIngredientsValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
