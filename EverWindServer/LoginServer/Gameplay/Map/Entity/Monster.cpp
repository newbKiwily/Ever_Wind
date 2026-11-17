#include "Monster.h"
#include <algorithm>
#include <cmath>
#include "../Map.h"
#include "../../CoreData/WorldTypes.h"
namespace EverWind::Gameplay
{
Monster::Monster()
{
    kind = EntityKind::Monster;
}
}

namespace EverWind::Gameplay
{
bool Monster::CanAttack() const
{
    return hp > 0 && !action.id;
}

void Monster::ApplyDamage(float amount)
{
    hp = std::max(0.0f, hp - std::max(0.0f, amount));

    if (hp <= 0)
    {
        state = MonsterState::Dead;

        targetId = 0;
    }
}

int Monster::UpdateAI(Map &space, const Skill &skill, double now, double dt)
{
    if (hp <= 0)
    {
        state = MonsterState::Dead;

        targetId = 0;

        return 0;
    }

    if (pushEnd > now)
    {
        state = MonsterState::Knockback;

        return 0;
    }

    if (action.id)
    {
        state = MonsterState::Attack;

        targetId = action.target;

        return 0;
    }

    auto *target = space.FindNearestPlayer(p, 10);

    targetId = target ? target->id : 0;

    if (target)
    {
        mode = Mode::Combat;

        if (space.InRange(*this, *target, skill.reach) && cooldowns[1] <= now)
        {
            state = MonsterState::Attack;

            velocity = {};

            cooldowns[1] = now + 1.8;

            return targetId;
        }

        state = MonsterState::Chase;

        velocity = {};

        if (!space.InRange(*this, *target, 1.2f))
        {
            MoveTowards(space, target->p, dt);
        }

        return 0;
    }

    mode = Mode::NonCombat;

    state = MonsterState::Wander;

    Vec3 goal{home.x + std::sin(static_cast<float>(now * .3 + id)) * 2, 0, home.z + std::cos(static_cast<float>(now * .3 + id)) * 2};

    MoveTowards(space, goal, dt);

    return 0;
}

void Monster::MoveTowards(Map &space, Vec3 goal, double dt)
{
    Vec3 delta{goal.x - p.x, 0, goal.z - p.z};

    float distance = Distance(goal, p);

    velocity = {};

    if (distance <= .2f)
    {
        return;
    }

    velocity = Scale(delta, speed / distance);

    if (!space.TryMove(*this, Add(p, Scale(velocity, static_cast<float>(dt)))))
    {
        velocity = {};
    }
}

bool Monster::CanRespawn(double now) const
{
    return hp <= 0 && respawnSeconds > 0 && now - diedAt >= respawnSeconds;
}

void Monster::Respawn(Vec3 position)
{
    hp = maxHp;

    mode = Mode::NonCombat;

    state = MonsterState::Idle;

    targetId = 0;

    p = position;

    home = p;

    velocity = {};

    cooldowns.clear();

    action = {};

    push = {};

    pushEnd = 0;

    pushId = 0;
}
}
