#pragma once
#include "Actor.h"
namespace EverWind::Gameplay
{
class StaticLevel final : public Actor
{
  public:
    Vec3 extent{};

    StaticLevel();

    Box Footprint() const;
};
}
