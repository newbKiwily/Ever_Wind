#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ItemUseValidator final : public IngameValidator
{
  public:
    explicit ItemUseValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
