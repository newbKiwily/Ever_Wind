#pragma once
#include "../Rule/GameRules.h"
#include <optional>
#include <random>
#include <sstream>
namespace EverWind::Gameplay
{
class WorldState
{
  public:
    explicit WorldState(const GameRules &definitions);
    void InitializeMaps(const std::string &directory);
    void InitializeWorldObjects();
    const GameRules &rules;
    std::unordered_map<int, std::shared_ptr<Player>> players;
    std::unordered_map<int, Map> maps;
    int nextPlacement = 100000000;
    double now = 0, nextSnapshot = 0;
    uint64_t nextAction = 1;
    std::mt19937 random{std::random_device{}()};
    std::optional<Vec3> MonsterSpawn(Map &map, float radius);
    Actor *Target(int id, int map);
    bool InRange(const Actor &a, const Actor &b, float reach) const;

  private:
    void ParseMapRow(const std::string &line);
    bool ParseMapFeature(const std::string &type, std::istringstream &stream);
    std::vector<std::string> objectRows;
    std::string sourcePath;
    bool mapsInitialized = false, objectsInitialized = false;
};
} // namespace EverWind::Gameplay
