#include "RefillSystem.h"
#include <cmath>
namespace EverWind::Gameplay
{
RefillSystem::RefillSystem(WorldState &state) : world(state)
{
}
Error RefillSystem::ValidateResource(const Context &c) const
{
    auto i = c.map.resources.find(c.request.target);
    if (i == c.map.resources.end())
        return Error::MissingTarget;
    const auto &r = i->second;
    if (r.stock > 0)
        return Error::None;
    if (r.refillDelay <= 0 || c.request.sequence != r.generation)
        return Error::Stale;
    if (c.now + 0.001 < r.refillAt)
        return Error::Cooldown;
    for (const auto &[id, p] : world.players)
        if (p->map == c.player.map && p->hp > 0 && std::abs(p->p.x - r.p.x) < p->radius + r.radius &&
            std::abs(p->p.z - r.p.z) < p->radius + r.radius)
            return Error::Busy;
    return Error::None;
}
bool RefillSystem::RefillResource(Resource &resource) const
{
    if (resource.stock > 0)
        return false;
    resource.stock = resource.capacity;
    resource.refillAt = 0;
    ++resource.generation;
    return true;
}
std::optional<Vec3> RefillSystem::MonsterPosition(Map &map, const Actor &monster)
{
    if (monster.hp > 0 || monster.respawnSeconds <= 0 ||
        world.now - monster.diedAt < monster.respawnSeconds)
        return std::nullopt;
    return map.monsterRadius > 0 ? world.MonsterSpawn(map, monster.radius)
                                 : std::optional<Vec3>{monster.home};
}
void RefillSystem::RefillMonster(Actor &m, Vec3 position) const
{
    m.hp = m.maxHp;
    m.mode = Mode::NonCombat;
    m.p = position;
    m.home = m.p;
    m.velocity = {};
    m.cooldowns.clear();
    m.action = {};
}
} // namespace EverWind::Gameplay
