#pragma once
#include "../Map/WorldState.h"
namespace EverWind::Gameplay
{
class RefillSystem
{
  public:
    explicit RefillSystem(WorldState &state);
    Error ValidateResource(const Context &context) const;
    bool RefillResource(Resource &resource) const;
    std::optional<Vec3> MonsterPosition(Map &map, const Actor &monster);
    void RefillMonster(Actor &monster, Vec3 position) const;

  private:
    WorldState &world;
};
} // namespace EverWind::Gameplay
