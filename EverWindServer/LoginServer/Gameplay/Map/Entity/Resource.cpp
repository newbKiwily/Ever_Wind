#include "Resource.h"
namespace EverWind::Gameplay
{
Resource::Resource()
{
    kind = EntityKind::Resource;

    radius = 0;
}

bool Resource::TryReserve(int playerId)
{
    if (playerId <= 0 || stock <= 0 || (owner && owner != playerId))
    {
        return false;
    }

    owner = playerId;

    return true;
}

bool Resource::Harvest(int playerId)
{
    if (owner != playerId || stock <= 0)
    {
        return false;
    }

    --stock;

    owner = 0;

    return true;
}
}
