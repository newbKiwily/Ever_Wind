#include "SkillExistsValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
SkillExistsValidator::SkillExistsValidator()
    : Validator("SkillExistsValidator")
{
}

Error SkillExistsValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    return c.player.FindSkill(c.request.definition) ? Error::None
                                                    : Error::BadRequest;
}
}
