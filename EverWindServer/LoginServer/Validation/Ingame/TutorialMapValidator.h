#pragma once
#include "../../Gameplay/Map/MapManager.h"
#include "../Validator.h"
namespace EverWind::Gameplay
{
class TutorialMapValidator final : public Validator
{
  private:
    MapManager &world;

    Error Validate(const ValidationContext &context) const override;
  public:
    TutorialMapValidator(MapManager &state);
};
}
