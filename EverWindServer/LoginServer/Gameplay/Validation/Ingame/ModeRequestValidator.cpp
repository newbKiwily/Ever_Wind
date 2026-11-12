#include "ModeRequestValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
ModeRequestValidator::ModeRequestValidator(WorldState &state) : IngameValidator(state, "ModeRequestValidator")
{
}
Error ModeRequestValidator::Validate(const Context &c) const
{
    return c.request.definition < 0 || c.request.definition > 1 ? Error::BadRequest
           : c.player.action.id                                 ? Error::Busy
                                                                : Error::None;
}
} // namespace EverWind::Gameplay
