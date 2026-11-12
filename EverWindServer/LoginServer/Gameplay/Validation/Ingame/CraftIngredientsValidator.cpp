#include "CraftIngredientsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
CraftIngredientsValidator::CraftIngredientsValidator(WorldState &state)
    : IngameValidator(state, "CraftIngredientsValidator")
{
}
Error CraftIngredientsValidator::Validate(const Context &c) const
{
    for (auto [id, n] : world.rules.recipes.at(c.request.definition).inputs)
    {
        auto i = c.player.inventory.find(id);
        if (i == c.player.inventory.end() || i->second < n)
            return Error::Ingredients;
    }
    return Error::None;
}
} // namespace EverWind::Gameplay
