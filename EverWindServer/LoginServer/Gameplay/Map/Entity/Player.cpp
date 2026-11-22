#include "Player.h"
#include <algorithm>
namespace EverWind::Gameplay
{
Player::Player()
{
    kind = EntityKind::Player;

    baseStats.attack = GrowthRules::PlayerAttack;

    baseStats.defence = GrowthRules::PlayerDefence;
}

void Player::BindDefinitions(const GameDataHub &data)
{
    skills.clear();
    quests.clear();
    tutorials.clear();
    // 정의 참조와 쿨다운을 보관하고 실제 습득 여부는 개인 성장 상태에서 확인한다.
    for (const auto &[definitionId, def] : data.skills)
    {
        skills.push_back({definitionId, &def});
    }

    for (const auto &[definitionId, def] : data.quests)
    {
        auto it = account.quests.find(definitionId);

        QuestProgress progress =
            it == account.quests.end() ? QuestProgress{} : it->second;
        progress.counts.resize(def.conditions.size());

        quests.push_back({definitionId, &def, std::move(progress)});
    }

    for (const auto &[definitionId, def] : data.tutorials)
    {
        tutorials.push_back({definitionId, &def});
    }
}

SkillState *Player::FindSkill(int definitionId)
{
    for (auto &v : skills)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}

const SkillState *Player::FindSkill(int definitionId) const
{
    for (auto &v : skills)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}

QuestStateEntry *Player::FindQuest(int definitionId)
{
    for (auto &v : quests)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}

const QuestStateEntry *Player::FindQuest(int definitionId) const
{
    for (auto &v : quests)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}

TutorialStateEntry *Player::FindTutorial(int definitionId)
{
    for (auto &v : tutorials)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}

const TutorialStateEntry *Player::FindTutorial(int definitionId) const
{
    for (auto &v : tutorials)
    {
        if (v.id == definitionId)
        {
            return &v;
        }
    }

    return nullptr;
}
}

namespace EverWind::Gameplay
{
unsigned Player::TutorialEvidence() const
{
    auto t = FindTutorial(tutorial);

    return t ? t->evidence : 0;
}

double Player::TutorialStart() const
{
    auto t = FindTutorial(tutorial);

    return t ? t->startedAt : 0;
}

void Player::BeginTutorial(double now)
{
    if (auto t = FindTutorial(tutorial))
    {
        t->evidence = 0;

        t->startedAt = now;
    }
}
}

namespace EverWind::Gameplay
{
bool Player::CanAttack() const
{
    return hp > 0 && !action.id;
}

void Player::ApplyDamage(float amount)
{
    hp = std::max(0.0f, hp - std::max(0.0f, amount));
}

int Player::AttackPower(const GameDataHub &data) const
{
    int result = baseStats.attack + growth.attackRank - 1;

    for (auto [slot, itemId] : equipment)
    {
        if (auto i = data.items.find(itemId); i != data.items.end() && slot == 1003)
        {
            result += i->second.power;
        }
    }

    return result;
}

int Player::DefencePower(const GameDataHub &data) const
{
    int result = baseStats.defence + growth.defenceRank - 1;

    for (auto [slot, itemId] : equipment)
    {
        if (auto i = data.items.find(itemId); i != data.items.end() && slot != 1003)
        {
            result += i->second.power;
        }
    }

    return result;
}
}

namespace EverWind::Gameplay
{
float Player::MoveSpeed() const
{
    return baseStats.moveSpeed * (1 + std::min(GrowthRules::MaxSpeedBonus, (growth.speedRank - 1) * GrowthRules::SpeedPerRank));
}

Skill Player::EffectiveSkill(int definitionId, const GameDataHub &data) const
{
    const auto state = FindSkill(definitionId);

    Skill result = state ? *state->definition : Skill{};

    const auto it = data.growth.skillRanks.find(definitionId);

    int rank = growth.SkillRank(definitionId);

    if (it != data.growth.skillRanks.end() && rank >= 1 && rank <= GrowthRules::MaxSkillRank)
    {
        const auto &definition = it->second[rank - 1];

        result.ratio = (definition.multiplier - 1) * 100;

        result.cooldown = definition.cooldown;
    }

    return result;
}
}
