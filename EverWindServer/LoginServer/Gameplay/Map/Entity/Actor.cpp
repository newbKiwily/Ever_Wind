#include "Actor.h"
#include "../Map.h"
#include "../../CoreData/WorldTypes.h"
namespace EverWind::Gameplay
{
Actor::~Actor() = default;

bool Actor::UpdateKnockback(Map &space, double now, double dt)
{
    if (pushEnd > now)
    {
        if (space.TryMove(*this, Add(p, Scale(push, static_cast<float>(dt)))))
        {
            velocity = push;
        }
        else
        {
            pushEnd = now;

            velocity = {};
        }

        return false;
    }

    if (!pushId)
    {
        return false;
    }

    pushId = 0;

    velocity = {};

    return true;
}
}
