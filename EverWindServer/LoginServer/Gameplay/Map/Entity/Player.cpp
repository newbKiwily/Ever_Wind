#include "Player.h"
#include <algorithm>
namespace EverWind::Gameplay
{
Player::Player()
{
    kind = EntityKind::Player;
}

void Player::BindDefinitions(const GameDataHub &data)
{
    skills.clear();
    quests.clear();
    tutorials.clear();
    // 정의된 스킬을 사용할 수 있는 기존 정책을 유지하며 플레이어별 스킬 해금 저장 테이블은 아직 없다.
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
}
