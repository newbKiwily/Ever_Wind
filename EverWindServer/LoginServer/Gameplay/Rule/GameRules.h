#pragma once
#include "../WorldTypes.h"
namespace EverWind::Gameplay
{
class GameRules
{
  public:
    static constexpr float PlayerReviveDelaySeconds = 10.0f;
    void Validate() const;
    std::unordered_map<int, Skill> skills;
    std::unordered_map<int, Recipe> recipes;
    std::unordered_map<int, ItemDefinition> items;
    std::unordered_map<int, QuestDefinition> quests;
    std::unordered_map<int, TutorialDefinition> tutorials;
    bool tutorialEnabled = true;
    int tutorialExitMapId = 1;
};
} // namespace EverWind::Gameplay
