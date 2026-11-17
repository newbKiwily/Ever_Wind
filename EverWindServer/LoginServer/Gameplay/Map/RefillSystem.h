#pragma once
namespace EverWind::Gameplay
{
struct RefillSystem;
}
#include "../CoreData/WorldTypes.h"
#include "MapManager.h"
namespace EverWind::Gameplay
{
struct RefillSystem
{
  private:
    MapManager &world;
  public:
    explicit RefillSystem(MapManager &state);

    Error ValidateResource(const Context &context) const;

    std::optional<Vec3> MonsterPosition(Map &map, const Monster &monster);
};
}
