#include "AttackCooldownValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
AttackCooldownValidator::AttackCooldownValidator(WorldState &state)
    : IngameValidator(state, "AttackCooldownValidator")
{
}
Error AttackCooldownValidator::Validate(const Context &c) const
{
    auto i = c.player.cooldowns.find(c.request.definition);
    return i != c.player.cooldowns.end() && i->second > c.now ? Error::Cooldown : Error::None;
}
} // namespace EverWind::Gameplay
