#pragma once
#include "../WorldTypes.h"
namespace EverWind::Gameplay
{
class GameRules
{
  public:
    void Initialize(const std::string &path);
    std::unordered_map<int, Skill> skills;
    std::unordered_map<int, Recipe> recipes;
    std::unordered_map<int, ItemDefinition> items;
    std::unordered_map<int, QuestDefinition> quests;
    bool production = false;
    float reviveDelay = 0, monsterRefill = 0;
    Vec3 tutorialMove{};
    float tutorialMoveRadius = 2;

    std::string sourcePath;
    std::vector<std::string> mapRows, objectRows;

  private:
    bool initialized = false;
};
} // namespace EverWind::Gameplay
