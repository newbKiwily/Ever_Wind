#pragma once
#include "Validator.h"
namespace EverWind::Gameplay
{
class PacketValidator final : public Validator
{
  private:
    Error (*rule)(const ValidationContext &);

    Error Validate(const ValidationContext &context) const override;
  public:
    using Rule = Error (*)(const ValidationContext &);

    static Error Envelope(const ValidationContext &context);

    static Error Contract(const ValidationContext &context);

    explicit PacketValidator(Rule rule = Contract);
};
}
