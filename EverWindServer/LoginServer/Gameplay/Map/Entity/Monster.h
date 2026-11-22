#pragma once
#include "Actor.h"
#include "IDamageable.h"
namespace EverWind::Gameplay
{
class Map;

struct Skill;

enum class MonsterState
{
    Idle,
    Wander,
    Chase,
    Attack,
    Knockback,
    Dead
};

class Monster final : public Actor, public IDamageable
{
  public:
    MonsterState state = MonsterState::Idle;

    int targetId = 0, attackSkill = 1;

    float attackCooldown = 1.8f;
    // 엔티티 편집기 연결 전 기본값이며 monsterstats 행으로 개별 지정할 수 있다.
    int experienceReward = 10;

    Vec3 home{};

    std::unordered_map<int, double> cooldowns;

    float respawnSeconds = 10;

    Monster();

    int UpdateAI(Map &map, const Skill &skill, double now, double dt);

    void MoveTowards(Map &map, Vec3 goal, double dt);

    bool CanRespawn(double now) const;

    void Respawn(Vec3 position);

    bool CanAttack() const;

    void ApplyDamage(float amount) override;
};
}
