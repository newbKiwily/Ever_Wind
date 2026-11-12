#include "MoveSequenceValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
MoveSequenceValidator::MoveSequenceValidator(WorldState &state)
    : IngameValidator(state, "MoveSequenceValidator")
{
}
Error MoveSequenceValidator::Validate(const Context &c) const
{
    return c.request.revision != c.player.revision || c.request.sequence <= c.player.sequence ? Error::Stale
                                                                                              : Error::None;
}
} // namespace EverWind::Gameplay
