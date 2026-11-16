#pragma once
#include "Actor.h"
#include "IDamageable.h"
namespace EverWind::Gameplay
{
class Monster final : public Actor, public IDamageable
{
  public:
    Vec3 home{};

    std::unordered_map<int, double> cooldowns;

    float respawnSeconds = 10;

    Monster();

    bool CanAttack() const;

    void ApplyDamage(float amount) override;
};
}
