#pragma once
#include "../../CoreData/GameDataHub.h"
#include "Actor.h"
#include "IDamageable.h"
namespace EverWind::Gameplay
{
struct SkillState
{
    int id;

    const Skill *definition;

    double cooldownUntil = 0;
};

struct QuestStateEntry
{
    int id;

    const QuestDefinition *definition;

    QuestProgress progress;
};

struct TutorialStateEntry
{
    int id;

    const TutorialDefinition *definition;

    unsigned evidence = 0;

    double startedAt = 0;
};

class Player final : public Actor, public IDamageable
{
  public:
    std::vector<SkillState> skills;

    std::vector<int> attackTargets;

    uint64_t reportedAttack = 0;

    std::vector<QuestStateEntry> quests;

    std::vector<TutorialStateEntry> tutorials;

    Send send;

    uint32_t epoch = 0, revision = 0, sequence = 0;

    uint64_t highRequest = 0;

    float budget = 1;

    double budgetTime = 0;

    std::unordered_map<int, int> inventory;

    std::unordered_map<uint64_t, CachedRequest> cache;

    std::deque<uint64_t> order;

    std::unordered_set<int> zones;

    AccountState account;

    std::function<bool(const AccountState &)> save;

    int baseAttack = 50, baseDefence = 20, tutorial = -1;

    double reviveAt = 0, lastSave = 0;

    bool dirty = false;

    std::unordered_map<int, int> equipment;

    Player();

    int AttackPower(const GameDataHub &data) const;

    int DefencePower(const GameDataHub &data) const;

    bool CanAttack() const;

    void ApplyDamage(float amount) override;

    unsigned TutorialEvidence() const;

    double TutorialStart() const;

    void BeginTutorial(double now);

    void BindDefinitions(const GameDataHub &data);

    SkillState *FindSkill(int id);
    const SkillState *FindSkill(int id) const;

    QuestStateEntry *FindQuest(int id);
    const QuestStateEntry *FindQuest(int id) const;

    TutorialStateEntry *FindTutorial(int id);
    const TutorialStateEntry *FindTutorial(int id) const;
};
}
