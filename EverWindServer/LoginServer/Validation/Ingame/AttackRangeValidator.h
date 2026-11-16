#pragma once
#include "../../Gameplay/Map/MapManager.h"
#include "../Validator.h"
namespace EverWind::Gameplay
{
class AttackRangeValidator final : public Validator
{
  private:
    MapManager &world;

    Error Validate(const ValidationContext &context) const override;
  public:
    AttackRangeValidator(MapManager &state);
};
}
