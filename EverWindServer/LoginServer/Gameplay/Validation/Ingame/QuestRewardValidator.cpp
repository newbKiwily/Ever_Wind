#include "QuestRewardValidator.h"
#include <cmath>
namespace EverWind::Gameplay
{
QuestRewardValidator::QuestRewardValidator(WorldState &state) : IngameValidator(state, "QuestRewardValidator")
{
}
Error QuestRewardValidator::Validate(const Context &c) const
{
    auto i = c.player.quests.find(c.request.definition);
    return !world.rules.quests.count(c.request.definition) || i == c.player.quests.end() ? Error::BadRequest
           : i->second.complete && !i->second.claimed                              ? Error::None
                                                                                   : Error::Restricted;
}
} // namespace EverWind::Gameplay
