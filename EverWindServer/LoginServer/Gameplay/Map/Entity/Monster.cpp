#include "Monster.h"
#include <algorithm>
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
}
}
