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

    int targetId = 0;

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
