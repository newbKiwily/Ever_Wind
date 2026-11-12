#pragma once
#include "../Map/WorldState.h"
#include "Validator.h"
namespace EverWind::Gameplay
{
class IngameValidator : public Validator
{
  public:
    explicit IngameValidator(WorldState &state, std::string_view name);
    virtual ~IngameValidator();

  protected:
    WorldState &world;
    virtual Error Validate(const Context &c) const override = 0;

  private:
    Error Validate(const PacketContext &c) const override;
};
} // namespace EverWind::Gameplay
