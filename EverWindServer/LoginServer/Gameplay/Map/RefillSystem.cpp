#include "RefillSystem.h"
#include <cmath>
namespace EverWind::Gameplay
{
RefillSystem::RefillSystem(MapManager &state)
    : world(state)
{
}

Error RefillSystem::ValidateResource(const Context &c) const
{
    auto i = c.map.resources.find(c.request.target);

    if (i == c.map.resources.end())
    {
        return Error::MissingTarget;
    }

    const auto &r = *i->second;

    if (r.stock > 0)
    {
        return Error::None;
    }

    if (auto error = r.ValidateRefill(c.request.sequence, c.now); error != Error::None)
    {
        return error;
    }

    for (const auto &[id, p] : world.Players())
    {
        if (p->map == c.player.map && p->hp > 0 && std::abs(p->p.x - r.p.x) < p->radius + r.radius && std::abs(p->p.z - r.p.z) < p->radius + r.radius)
        {
            return Error::Busy;
        }
    }

    return Error::None;
}

std::optional<Vec3> RefillSystem::MonsterPosition(Map &map, const Monster &monster)
{
    if (!monster.CanRespawn(world.now))
    {
        return std::nullopt;
    }

    return map.monsterRadius > 0 ? world.MonsterSpawn(map, monster.radius, monster.id)
                                 : std::optional<Vec3>{monster.home};
}
}
