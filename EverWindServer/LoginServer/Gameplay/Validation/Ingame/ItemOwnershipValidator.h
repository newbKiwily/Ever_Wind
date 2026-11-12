#pragma once
#include "../IngameValidator.h"
namespace EverWind::Gameplay
{
class ItemOwnershipValidator final : public IngameValidator
{
  public:
    explicit ItemOwnershipValidator(WorldState &state);

  private:
    Error Validate(const Context &c) const override;
};
} // namespace EverWind::Gameplay
