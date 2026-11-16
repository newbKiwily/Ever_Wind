#include "QuestRewardValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
QuestRewardValidator::QuestRewardValidator()
    : Validator("QuestRewardValidator")
{
}

Error QuestRewardValidator::Validate(const ValidationContext &context) const
{
    if (!context.game)
    {
        return Error::BadRequest;
    }

    const auto &c = *context.game;

    auto entry = c.player.FindQuest(c.request.definition);

    return !entry ? Error::BadRequest
           : entry->progress.complete && !entry->progress.claimed
               ? Error::None
               : Error::Restricted;
}
}
