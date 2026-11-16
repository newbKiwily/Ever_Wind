#pragma once
#include "../../Gameplay/Map/MapManager.h"
#include "../Validator.h"
namespace EverWind::Gameplay
{
class ItemExistsValidator final : public Validator
{
  private:
    MapManager &world;

    Error Validate(const ValidationContext &context) const override;
  public:
    ItemExistsValidator(MapManager &state);
};
}
