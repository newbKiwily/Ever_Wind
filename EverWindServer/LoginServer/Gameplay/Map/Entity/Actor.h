#pragma once
#include "../../CoreData/GameplayData.h"
namespace EverWind::Gameplay
{
class Map;

class Actor
{
  public:
    int id = 0, map = 1, visual = 0;

    EntityKind kind = EntityKind::Player;

    Vec3 p{};

    float radius = .45f;

    bool istrigger = true;

    Vec3 velocity{};

    float speed = 5, hp = 100, maxHp = 100;

    Mode mode = Mode::NonCombat;

    Action action{};

    Vec3 push{};

    double pushEnd = 0;

    uint64_t pushId = 0;

    int consecutiveHits = 0;

    double hitReactionEnd = 0;
    double diedAt = 0;

    HitReaction RegisterHit(double now);

    void StartKnockback(Vec3 source, double now);

    void ResetHitReaction();

    bool UpdateKnockback(Map &map, double now, double dt);

    virtual ~Actor();
};
}
