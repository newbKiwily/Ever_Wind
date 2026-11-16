#pragma once
namespace EverWind::Gameplay
{
struct IDamageable
{
    virtual ~IDamageable() = default;

    virtual void ApplyDamage(float amount) = 0;
};
}
