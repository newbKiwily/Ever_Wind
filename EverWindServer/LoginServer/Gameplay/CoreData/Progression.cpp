#include "Progression.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace EverWind::Gameplay
{
GrowthRules::GrowthRules()
    : experienceToNext{100, 140, 190, 250, 320, 400, 490, 590, 700, 820, 950, 1090, 1240, 1400, 1570, 1750, 1940, 2140, 2350, 0}
{
    skillRanks.emplace(1, std::array<SkillRankDefinition, 5>{{{1, 1}, {1.1f, .95f}, {1.2f, .9f}, {1.3f, .85f}, {1.4f, .8f}}});
    skillRanks.emplace(2, std::array<SkillRankDefinition, 5>{{{1.5f, 2}, {1.75f, 1.9f}, {2, 1.8f}, {2.25f, 1.7f}, {2.5f, 1.6f}}});
    skillRanks.emplace(3, std::array<SkillRankDefinition, 5>{{{1, 3}, {1.1f, 2.8f}, {1.2f, 2.6f}, {1.3f, 2.4f}, {1.4f, 2.2f}}});
}

void GrowthRules::Validate() const
{
    for (int i = 0; i < MaxLevel - 1; ++i)
    {
        if (experienceToNext[i] <= 0)
        {
            throw std::runtime_error("Invalid level experience table");
        }
    }

    if (experienceToNext.back() != 0)
    {
        throw std::runtime_error("Max level cannot require experience");
    }

    for (const auto &[id, ranks] : skillRanks)
    {
        for (const auto &rank : ranks)
        {
            if (id <= 0 || !std::isfinite(rank.multiplier) || !std::isfinite(rank.cooldown) || rank.multiplier < 0 || rank.cooldown < 0)
            {
                throw std::runtime_error("Invalid skill rank table");
            }
        }
    }
}

int GrowthState::SkillRank(int id) const
{
    const auto it = skillRanks.find(id);

    return it == skillRanks.end() ? 0 : it->second;
}

bool GrowthState::Valid(const GrowthRules &rules) const
{
    if (level < 1 || level > GrowthRules::MaxLevel || experience < 0 || skillPoints < 0 || statPoints < 0 || attackRank < 1 || defenceRank < 1 || speedRank < 1 || speedRank > GrowthRules::MaxSpeedRank || SkillRank(1) < 1)
    {
        return false;
    }

    if (level == GrowthRules::MaxLevel ? experience != 0 : experience >= rules.experienceToNext[level - 1])
    {
        return false;
    }

    int64_t skillSpent = 0;

    for (const auto &[id, rank] : skillRanks)
    {
        if (id <= 0 || rank < 0 || rank > GrowthRules::MaxSkillRank)
        {
            return false;
        }

        skillSpent += rank - (id == 1 ? 1 : 0);
    }

    const int64_t statSpent = static_cast<int64_t>(attackRank) + defenceRank + speedRank - 3;

    return skillSpent + skillPoints == GrowthRules::InitialSkillPoints + (level - 1) * GrowthRules::SkillPointsPerLevel && statSpent + statPoints == GrowthRules::InitialStatPoints + (level - 1) * GrowthRules::StatPointsPerLevel;
}

bool GrowthState::GainExperience(int amount, const GrowthRules &rules)
{
    if (amount <= 0 || level >= GrowthRules::MaxLevel)
    {
        return false;
    }

    int64_t total = static_cast<int64_t>(experience) + amount;

    while (level < GrowthRules::MaxLevel && total >= rules.experienceToNext[level - 1])
    {
        total -= rules.experienceToNext[level - 1];

        ++level;

        skillPoints += GrowthRules::SkillPointsPerLevel;

        statPoints += GrowthRules::StatPointsPerLevel;
    }

    experience = level == GrowthRules::MaxLevel ? 0 : static_cast<int>(total);

    return true;
}

Error GrowthState::UpgradeStat(StatKind stat)
{
    int *rank = stat == StatKind::Attack ? &attackRank : stat == StatKind::Defence ? &defenceRank
                                                     : stat == StatKind::MoveSpeed ? &speedRank
                                                                                   : nullptr;
    if (!rank)
    {
        return Error::BadRequest;
    }

    if (stat == StatKind::MoveSpeed && *rank >= GrowthRules::MaxSpeedRank)
    {
        return Error::RankLimit;
    }

    if (statPoints <= 0)
    {
        return Error::InsufficientPoints;
    }

    --statPoints;

    ++*rank;

    return Error::None;
}

Error GrowthState::UpgradeSkill(int id, const GrowthRules &rules)
{
    if (!rules.skillRanks.count(id))
    {
        return Error::BadRequest;
    }

    if (SkillRank(id) >= GrowthRules::MaxSkillRank)
    {
        return Error::RankLimit;
    }

    if (skillPoints <= 0)
    {
        return Error::InsufficientPoints;
    }

    --skillPoints;

    ++skillRanks[id];

    return Error::None;
}

void GrowthState::ResetStats()
{
    statPoints = GrowthRules::InitialStatPoints + (level - 1) * GrowthRules::StatPointsPerLevel;

    attackRank = defenceRank = speedRank = 1;
}

void GrowthState::ResetSkills()
{
    skillPoints = GrowthRules::InitialSkillPoints + (level - 1) * GrowthRules::SkillPointsPerLevel;

    skillRanks.clear();

    skillRanks.emplace(1, 1);
}
}
