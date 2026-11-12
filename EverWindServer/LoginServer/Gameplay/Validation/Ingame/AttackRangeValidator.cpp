#include "AttackRangeValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
AttackRangeValidator::AttackRangeValidator(WorldState &state) : IngameValidator(state, "AttackRangeValidator")
{
}
Error AttackRangeValidator::Validate(const Context &c) const
{
    return world.InRange(c.player, *world.Target(c.request.target, c.player.map),
                         world.rules.skills.at(c.request.definition).reach)
               ? Error::None
               : Error::OutOfRange;
}
} // namespace EverWind::Gameplay
