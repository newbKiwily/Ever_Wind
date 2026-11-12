#include "GameRules.h"
#include <cmath>
#include <stdexcept>
namespace EverWind::Gameplay
{
void GameRules::Validate() const
{
    if (skills.empty() || items.empty())
        throw std::runtime_error("Missing DB game definitions");
    for (const auto &[id, s] : skills)
        if (!std::isfinite(s.duration) || !std::isfinite(s.hit) || !std::isfinite(s.reach) ||
            !std::isfinite(s.cooldown) || s.duration < 0 || s.hit < 0 || s.hit > s.duration || s.reach < 0 ||
            s.cooldown < 0)
            throw std::runtime_error("Invalid DB skill " + std::to_string(id));
    for (const auto &[id, r] : recipes)
    {
        if (!items.count(r.output) || r.count <= 0 || r.duration < 0)
            throw std::runtime_error("Invalid DB recipe");
        for (const auto &[item, count] : r.inputs)
            if (!items.count(item) || count <= 0)
                throw std::runtime_error("Invalid DB recipe ingredient");
    }
    for (const auto &[id, q] : quests)
    {
        for (const auto &[item, count] : q.rewards)
            if (!items.count(item) || count <= 0)
                throw std::runtime_error("Invalid DB quest reward");
        for (const auto &c : q.conditions)
            if (c.required <= 0)
                throw std::runtime_error("Invalid DB quest condition");
    }
    if (tutorialEnabled && !tutorials.count(0))
        throw std::runtime_error("Missing DB tutorial definitions");
    for (const auto &[id, t] : tutorials)
        if (id < 0 || id >= 32 || (t.next != -1 && !tutorials.count(t.next)) || t.minimumSeconds < 0 ||
            t.radius < 0)
            throw std::runtime_error("Invalid DB tutorial definition");
}
} // namespace EverWind::Gameplay
