#include "ResourceRefillValidator.h"
#include "../../Refill/RefillSystem.h"
namespace EverWind::Gameplay
{
ResourceRefillValidator::ResourceRefillValidator(WorldState &state, RefillSystem &refillSystem) : IngameValidator(state, "ResourceRefillValidator"), refill(refillSystem) {}
Error ResourceRefillValidator::Validate(const Context &c) const { return refill.ValidateResource(c); }
}
