#pragma once
#include "Validator.h"
namespace EverWind::Gameplay
{
class PacketValidator final : public Validator
{
  public:
    PacketValidator();

  private:
    Error Validate(const PacketContext &context) const override;
    Error Validate(const Context &context) const override;
};
} // namespace EverWind::Gameplay
