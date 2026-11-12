#include "PacketValidation.h"
#include "Validation/PacketValidator.h"
namespace EverWind::Gameplay
{
PacketValidation::PacketValidation()
{
    Initialize();
}
void PacketValidation::Initialize()
{
    basic = std::make_unique<PacketValidator>();
}
Error PacketValidation::Envelope(const PacketContext &c) const
{
    auto context = c;
    context.phase = PacketCheckPhase::Envelope;
    return basic->Check(context);
}
Error PacketValidation::Contract(const Request &request) const
{
    PacketContext c;
    c.phase = PacketCheckPhase::Contract;
    c.request = request;
    return basic->Check(c);
}
} // namespace EverWind::Gameplay
