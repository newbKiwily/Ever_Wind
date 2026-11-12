#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class RecipeExistsValidator final : public IngameValidator
{
  public:
    explicit RecipeExistsValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
