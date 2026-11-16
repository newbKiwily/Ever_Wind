#include "StaticLevel.h"
namespace EverWind::Gameplay
{
StaticLevel::StaticLevel()
{
    kind = EntityKind::Wall;

    istrigger = false;
}

Box StaticLevel::Footprint() const
{
    return {id, p.x, p.z, extent.x, extent.z};
}
}
