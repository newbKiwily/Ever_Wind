#include "WorldEpochValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
WorldEpochValidator::WorldEpochValidator(WorldState &state) : IngameValidator(state, "WorldEpochValidator")
{
}
Error WorldEpochValidator::Validate(const Context &c) const
{
    return c.player.epoch != c.request.epoch ? Error::WrongWorld : Error::None;
}
} // namespace EverWind::Gameplay
