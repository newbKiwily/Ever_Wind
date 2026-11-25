#include "PacketValidator.h"
#include "../Network/GameplayPackets.h"
namespace EverWind::Gameplay
{
PacketValidator::PacketValidator(Rule value)
    : Validator("PacketValidator"), rule(value)
{
}

Error PacketValidator::Validate(const ValidationContext &c) const
{
    return rule ? rule(c) : Error::BadRequest;
}

Error PacketValidator::Envelope(const ValidationContext &c)
{
    size_t expected = RequestPayloadSize(c.id);

    if (c.id == static_cast<uint16_t>(NetPackets::PacketId::C2S_LOGIN_REQ))
    {
        expected = sizeof(NetPackets::PKT_LOGIN_REQ);
    }
    else if (c.id == static_cast<uint16_t>(NetPackets::PacketId::C2S_SIGNUP_REQ))
    {
        expected = sizeof(NetPackets::PKT_SIGNUP_REQ);
    }

    if (!expected || !c.payload || c.size != expected)
    {
        return Error::BadRequest;
    }

    return RequestPayloadSize(c.id) && !c.authenticated ? Error::Unauthenticated
                                                        : Error::None;
}

Error PacketValidator::Contract(const ValidationContext &c)
{
    if (!c.request)
    {
        return Error::BadRequest;
    }

    const auto &r = *c.request;

    if (r.targetCount > MaxAttackTargets || (r.kind != RequestKind::AttackTargets && r.targetCount != 0))
    {
        return Error::BadRequest;
    }

    for (uint16_t i = 0; i < r.targetCount; ++i)
    {
        if (r.targets[i] <= 0)
        {
            return Error::BadRequest;
        }
    }

    return !r.requestId ||
                   static_cast<int>(r.kind) < 1 ||
                   static_cast<int>(r.kind) >
                       static_cast<int>(RequestKind::ResetSkills)
               ? Error::BadRequest
               : Error::None;
}
}
