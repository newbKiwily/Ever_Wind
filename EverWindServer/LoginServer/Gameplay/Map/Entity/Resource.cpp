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

bool Resource::Harvest(int playerId, double now)
{
    if (owner != playerId || stock <= 0)
    {
        return false;
    }

    --stock;

    if (stock == 0 && refillDelay > 0)
    {
        refillAt = now + refillDelay;

        ++generation;
    }

    owner = 0;

    return true;
}

void Resource::Release(int playerId)
{
    if (owner == playerId)
    {
        owner = 0;
    }
}

Error Resource::ValidateRefill(unsigned requestedGeneration, double now) const
{
    if (stock > 0)
    {
        return Error::None;
    }

    if (refillDelay <= 0 || requestedGeneration != generation)
    {
        return Error::Stale;
    }

    return now + .001 < refillAt ? Error::Cooldown : Error::None;
}

bool Resource::Refill()
{
    if (stock > 0)
    {
        return false;
    }

    stock = capacity;

    refillAt = 0;

    ++generation;

    return true;
}
}
