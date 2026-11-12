#include "PacketValidator.h"
namespace EverWind::Gameplay
{
PacketValidator::PacketValidator() : Validator("PacketValidator")
{
}
Error PacketValidator::Validate(const PacketContext &context) const
{
    if (context.phase == PacketCheckPhase::Envelope)
    {
        if (context.id != RequestPacket || context.size != sizeof(Request) || !context.payload)
            return Error::BadRequest;
        return context.authenticated ? Error::None : Error::Unauthenticated;
    }
    if (context.phase != PacketCheckPhase::Contract)
        return Error::BadRequest;
    const auto &request = context.request;
    return request.version != ProtocolVersion || !request.requestId || static_cast<int>(request.kind) < 1 ||
                   static_cast<int>(request.kind) > static_cast<int>(RequestKind::Logout)
               ? Error::BadRequest
               : Error::None;
}

Error PacketValidator::Validate(const Context &) const
{
    return Error::BadRequest;
}
} // namespace EverWind::Gameplay
