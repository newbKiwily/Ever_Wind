#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ItemExistsValidator final : public IngameValidator
{
  public:
    explicit ItemExistsValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
