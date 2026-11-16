#include "CraftIngredientsValidator.h"
#include "../../Gameplay/Map/MapManager.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
CraftIngredientsValidator::CraftIngredientsValidator(MapManager &state)
    : Validator("CraftIngredientsValidator"), world(state)
{
}

Error CraftIngredientsValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto recipe = world.Data().recipes.find(c.request.definition);

    if (recipe == world.Data().recipes.end())
    {
        return Error::BadRequest;
    }

    for (auto [id, n] : recipe->second.inputs)
    {
        auto i = c.player.inventory.find(id);

        if (i == c.player.inventory.end() || i->second < n)
        {
            return Error::Ingredients;
        }
    }

    return Error::None;
}
}
