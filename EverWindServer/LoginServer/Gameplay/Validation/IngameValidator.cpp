#include "IngameValidator.h"
namespace EverWind::Gameplay
{
IngameValidator::IngameValidator(WorldState &state, std::string_view name) : Validator(name), world(state)
{
}
IngameValidator::~IngameValidator() = default;
Error IngameValidator::Validate(const PacketContext &) const
{
    return Error::BadRequest;
}
} // namespace EverWind::Gameplay
