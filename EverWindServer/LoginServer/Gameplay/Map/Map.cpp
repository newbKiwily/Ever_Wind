#include "Map.h"
#include <algorithm>
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
}
