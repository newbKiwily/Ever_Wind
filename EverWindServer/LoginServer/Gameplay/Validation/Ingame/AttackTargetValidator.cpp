#include "AttackTargetValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
AttackTargetValidator::AttackTargetValidator(WorldState &state)
    : IngameValidator(state, "AttackTargetValidator")
{
}
Error AttackTargetValidator::Validate(const Context &c) const
{
    auto t = world.Target(c.request.target, c.player.map);
    return !t || t->hp <= 0 || t->id == c.player.id ? Error::MissingTarget : Error::None;
}
} // namespace EverWind::Gameplay
