#pragma once
#include "Validation/PacketValidator.h"
namespace EverWind::Gameplay
{
class PacketValidation
{
  public:
    PacketValidation();
    Error Envelope(const PacketContext &context) const;
    Error Contract(const Request &request) const;

  private:
    void Initialize();
    std::unique_ptr<Validator> basic;
};
} // namespace EverWind::Gameplay
