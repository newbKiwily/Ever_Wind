#include "SkillExistsValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
SkillExistsValidator::SkillExistsValidator(WorldState &state) : IngameValidator(state, "SkillExistsValidator")
{
}
Error SkillExistsValidator::Validate(const Context &c) const
{
    return world.rules.skills.count(c.request.definition) ? Error::None : Error::BadRequest;
}
} // namespace EverWind::Gameplay
