#pragma once
#include "Entity/Monster.h"
#include "Entity/Player.h"
#include "Entity/Resource.h"
#include "Entity/StaticLevel.h"
namespace EverWind::Gameplay
{
class Map
{
  public:
    std::vector<std::shared_ptr<Actor>> actors;
    // 종류별 목록은 비소유 인덱스이며 실제 개체의 소유권은 개체 목록에 있다.
    std::unordered_map<int, Player *> players;

    std::unordered_map<int, StaticLevel *> levels;

    int id = 0, version = 1;

    float defaultMonsterRespawnSeconds = 10;
    float radius = 24;

    std::vector<Zone> zones;

    std::unordered_map<int, Monster *> monsters;

    std::unordered_map<int, Resource *> resources;

    Vec3 center{}, spawn{0, 0, -10};

    HeightField surface;

    Vec3 monsterCenter{};

    float monsterRadius = 0;

    std::unordered_map<int, StaticLevel *> placements;

    void AddActor(std::shared_ptr<Actor> actor);

    void RemovePlayer(int id);

    std::shared_ptr<Player> FindPlayer(int id) const;
};
}
