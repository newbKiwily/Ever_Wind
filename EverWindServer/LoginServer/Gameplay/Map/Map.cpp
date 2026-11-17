#include "Map.h"
#include <algorithm>
#include <cmath>
#include "../CoreData/WorldTypes.h"
#include <stdexcept>
namespace EverWind::Gameplay
{
void Map::AddActor(std::shared_ptr<Actor> actor)
{
    if (!actor)
    {
        throw std::invalid_argument("Null actor");
    }

    for (const auto &a : actors)
    {
        if (a->id == actor->id)
        {
            throw std::runtime_error("Duplicate actor ID in map");
        }
    }

    actors.reserve(actors.size() + 1);

    if (auto p = dynamic_cast<Player *>(actor.get()))
    {
        players.emplace(p->id, p);
    }
    else if (auto m = dynamic_cast<Monster *>(actor.get()))
    {
        monsters.emplace(m->id, m);
    }
    else if (auto r = dynamic_cast<Resource *>(actor.get()))
    {
        resources.emplace(r->id, r);
    }
    else if (auto l = dynamic_cast<StaticLevel *>(actor.get()))
    {
        if (l->kind == EntityKind::Placement)
        {
            placements.emplace(l->id, l);
        }
        else
        {
            levels.emplace(l->id, l);
        }
    }

    actors.push_back(actor);

    actor->map = id;
}

void Map::RemovePlayer(int playerId)
{
    players.erase(playerId);

    std::erase_if(actors, [playerId](const auto &a)
    {
        return a->id == playerId && a->kind == EntityKind::Player;
    });
}

std::shared_ptr<Player> Map::FindPlayer(int playerId) const
{
    if (!players.count(playerId))
    {
        return {};
    }

    for (const auto &a : actors)
    {
        if (a.get() == players.at(playerId))
        {
            return std::static_pointer_cast<Player>(a);
        }
    }

    return {};
}

Player *Map::FindNearestPlayer(Vec3 position, float range) const
{
    Player *target = nullptr;

    for (const auto &[playerId, player] : players)
    {
        if (!player->epoch || player->map != this->id || player->hp <= 0)
        {
            continue;
        }

        float distance = Distance(position, player->p);

        if (distance < range)
        {
            range = distance;

            target = player;
        }
    }

    return target;
}

bool Map::InRange(const Actor &a, const Actor &b, float reach) const
{
    return std::abs(a.p.y - b.p.y) < 2 && Distance(a.p, b.p) <= a.radius + b.radius + reach;
}

bool Map::TryMove(Actor &actor, Vec3 position) const
{
    if (actor.map != id)
    {
        return false;
    }

    position.y = surface.Sample(position.x, position.z);

    if (Geometry(*this, actor.p, position, actor.radius, actor.id) != Error::None)
    {
        return false;
    }

    actor.p = position;

    return true;
}
}
