#pragma once
#include "Protocol.h"
#include <array>
#include <cstdint>
#include <unordered_map>
namespace EverWind::Gameplay
{
enum class StatKind : int
{
    Attack = 1,
    Defence,
    MoveSpeed
};

struct SkillRankDefinition
{
    float multiplier = 1, cooldown = 1;
};

struct GrowthRules
{
    static constexpr int MaxLevel = 20, MaxSkillRank = 5, MaxSpeedRank = 5;
    static constexpr int InitialSkillPoints = 3, InitialStatPoints = 3;
    static constexpr int SkillPointsPerLevel = 1, StatPointsPerLevel = 3;
    static constexpr int PlayerAttack = 10, PlayerDefence = 3;

    static constexpr float SpeedPerRank = .075f, MaxSpeedBonus = .30f;

    std::array<int, MaxLevel> experienceToNext;

    int playerAttack = PlayerAttack, playerDefence = PlayerDefence;

    std::unordered_map<int, std::array<SkillRankDefinition, MaxSkillRank>> skillRanks;

    GrowthRules();

    void Validate() const;
};

struct GrowthState
{
    int level = 1, experience = 0;
    int skillPoints = GrowthRules::InitialSkillPoints, statPoints = GrowthRules::InitialStatPoints;
    int attackRank = 1, defenceRank = 1, speedRank = 1;

    std::unordered_map<int, int> skillRanks{{1, 1}};

    int SkillRank(int id) const;

    bool Valid(const GrowthRules &rules) const;

    bool GainExperience(int amount, const GrowthRules &rules);

    Error UpgradeStat(StatKind stat);

    Error UpgradeSkill(int id, const GrowthRules &rules);

    void ResetStats();

    void ResetSkills();
};
}
