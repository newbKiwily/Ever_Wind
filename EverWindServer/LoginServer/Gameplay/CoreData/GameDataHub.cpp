#include "GameDataHub.h"
#include <cmath>
#include <stdexcept>
namespace EverWind::Gameplay
{
void GameDataHub::Validate() const
{
    growth.Validate();

    if (growth.playerAttack < 0 || growth.playerDefence < 0 || !skills.count(1))
    {
        throw std::runtime_error("Invalid player definition");
    }

    for (const auto &[id, m] : monsters)
    {
        if (!skills.count(m.attackSkill) || (skills.at(m.attackSkill).kind != SkillKind::SingleTarget && skills.at(m.attackSkill).kind != SkillKind::Ranged) || !std::isfinite(m.attackCooldown) || m.attackCooldown < 0 || id <= 0 || m.visual < 0 || m.stats.attack < 0 || m.stats.defence < 0 || m.experience < 0 || !std::isfinite(m.stats.moveSpeed) || m.stats.moveSpeed < 0 || !std::isfinite(m.hp) || m.hp <= 0 || !std::isfinite(m.radius) || m.radius <= 0)
        {
            throw std::runtime_error("Invalid monster definition");
        }
    }

    for (const auto &[id, r] : resources)
    {
        if (id <= 0 || !items.count(r.item) || r.capacity <= 0 || !std::isfinite(r.radius) || r.radius <= 0 || !std::isfinite(r.gatherTime) || r.gatherTime < 0 || !std::isfinite(r.refillDelay) || r.refillDelay < 0)
        {
            throw std::runtime_error("Invalid resource definition");
        }
    }

    std::unordered_set<std::string> itemKeys;

    for (const auto &[id, item] : items)
    {
        if (id <= 0 || item.key.empty() || !itemKeys.emplace(item.key).second || !std::isfinite(item.heal) || item.heal < 0)
        {
            throw std::runtime_error("Invalid item definition");
        }
    }

    if (skills.empty() || items.empty())
    {
        throw std::runtime_error("Missing content game definitions");
    }

    for (const auto &[id, s] : skills)
    {
        if (id <= 0 || !std::isfinite(s.push) || !std::isfinite(s.duration) || !std::isfinite(s.hit) || !std::isfinite(s.reach) || !std::isfinite(s.cooldown) || !std::isfinite(s.area) || !std::isfinite(s.ratio) || !std::isfinite(s.damage) || s.duration <= 0 || s.hit < 0 || s.hit > s.duration || s.reach < 0 || s.cooldown < 0 || s.area < 0 || static_cast<unsigned>(s.kind) > static_cast<unsigned>(SkillKind::Buff) || (s.kind == SkillKind::Area && s.area <= 0))
        {
            throw std::runtime_error("Invalid content skill " + std::to_string(id));
        }
    }

    for (const auto &[id, r] : recipes)
    {
        if (id <= 0 || !items.count(r.output) || r.count <= 0 || !std::isfinite(r.duration) || r.duration < 0)
        {
            throw std::runtime_error("Invalid content recipe");
        }

        for (const auto &[item, count] : r.inputs)
        {
            if (!items.count(item) || count <= 0)
            {
                throw std::runtime_error("Invalid content recipe ingredient");
            }
        }
    }

    for (const auto &[id, q] : quests)
    {
        if (id <= 0)
        {
            throw std::runtime_error("Invalid quest ID");
        }

        for (const auto &condition : q.conditions)
        {
            if (condition.kind < 0 || condition.kind > 4 || condition.required <= 0)
            {
                throw std::runtime_error("Invalid quest condition");
            }
        }

        for (const auto &[item, count] : q.rewards)
        {
            if (!items.count(item) || count <= 0)
            {
                throw std::runtime_error("Invalid content quest reward");
            }
        }
    }

    if (tutorialEnabled && !tutorials.count(0))
    {
        throw std::runtime_error("Missing content tutorial definitions");
    }

    for (const auto &[id, t] : tutorials)
    {
        if (id < 0 || id >= 32 || (t.next != -1 && !tutorials.count(t.next)) || !std::isfinite(t.minimumSeconds) || (!std::isfinite(t.position.x) || !std::isfinite(t.position.y) || !std::isfinite(t.position.z)) || !std::isfinite(t.radius) || t.minimumSeconds < 0 || t.radius < 0)
        {
            throw std::runtime_error("Invalid content tutorial definition");
        }
    }
}
}
