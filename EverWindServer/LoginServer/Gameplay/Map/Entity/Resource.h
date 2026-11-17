#pragma once
#include "Actor.h"
namespace EverWind::Gameplay
{
class Resource final : public Actor
{
  public:
    int item = 0, stock = 0, owner = 0;
    int capacity = 1;

    unsigned generation = 0;

    float gatherTime = 2, refillDelay = 0;

    double refillAt = 0;

    Resource();

    bool TryReserve(int playerId);

    bool Harvest(int playerId, double now);

    void Release(int playerId);

    Error ValidateRefill(unsigned requestedGeneration, double now) const;

    bool Refill();
};
}
