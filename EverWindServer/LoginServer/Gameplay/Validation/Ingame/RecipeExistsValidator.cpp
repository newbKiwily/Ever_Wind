#include "RecipeExistsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
RecipeExistsValidator::RecipeExistsValidator(WorldState &state)
    : IngameValidator(state, "RecipeExistsValidator")
{
}
Error RecipeExistsValidator::Validate(const Context &c) const
{
    return world.rules.recipes.count(c.request.definition) ? Error::None : Error::BadRequest;
}
} // namespace EverWind::Gameplay
