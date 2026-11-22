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

    if (!c.player.FindSkill(c.request.definition))
    {
        return Error::BadRequest;
    }

    return c.player.growth.SkillRank(c.request.definition) > 0 ? Error::None : Error::UnlearnedSkill;
}
}
