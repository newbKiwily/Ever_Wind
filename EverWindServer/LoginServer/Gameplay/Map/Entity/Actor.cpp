#include "Actor.h"
#include "../../CoreData/GameDataHub.h"
#include <algorithm>
#include "../Map.h"
#include "../../CoreData/WorldTypes.h"
namespace EverWind::Gameplay
{
Actor::~Actor() = default;

bool Actor::UpdateKnockback(Map &space, double now, double dt)
{
    // 마지막 틱이 종료 시각을 지나도 남은 이동 구간까지만 적용한다.
    double step = pushId ? std::max(0.0, std::min(now, pushEnd) - std::max(now - dt, pushEnd - GameDataHub::KnockbackSeconds)) : 0;

    if (step > 0)
    {
        if (space.TryMove(*this, Add(p, Scale(push, static_cast<float>(step)))))
        {
            velocity = push;
        }
        else
        {
            pushEnd = now;

            velocity = {};
        }
    }

    if (pushEnd > now)
    {
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

HitReaction Actor::RegisterHit(double now)
{
    if (pushEnd > now)
    {
        return HitReaction::DamageOnly;
    }

    // 피격 모션 종료와 누적 횟수를 분리해 세 번째 유효 피격에서 넉백한다.
    ++consecutiveHits;

    if (consecutiveHits >= GameDataHub::ConsecutiveHitThreshold)
    {
        ResetHitReaction();

        return HitReaction::Knockback;
    }

    hitReactionEnd = now + (kind == EntityKind::Monster ? GameDataHub::MonsterHitSeconds : GameDataHub::PlayerHitSeconds);

    return HitReaction::Hit;
}

void Actor::StartKnockback(Vec3 source, double now)
{
    Vec3 direction{p.x - source.x, 0, p.z - source.z};

    float distance = Distance(p, source);

    if (distance < .001f)
    {
        direction = {1, 0, 0};

        distance = 1;
    }

    push = Scale(direction, GameDataHub::KnockbackDistance / (distance * GameDataHub::KnockbackSeconds));

    pushEnd = now + GameDataHub::KnockbackSeconds;
}

void Actor::ResetHitReaction()
{
    consecutiveHits = 0;

    hitReactionEnd = 0;
}
}
