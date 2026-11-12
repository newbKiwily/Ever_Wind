#include "MovementBudgetValidator.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay {
MovementBudgetValidator::MovementBudgetValidator(WorldState &state)
    : IngameValidator(state, "MovementBudgetValidator") {}
Error MovementBudgetValidator::Validate(const Context &c) const {
  const float budget = std::min(
      1.0f, c.player.budget + static_cast<float>(c.now - c.player.budgetTime) *
                                  c.player.speed * std::sqrt(2.0f));
  return Distance(c.player.p, c.request.position) > budget + 0.001f
             ? Error::Speed
             : Error::None;
}
} // namespace EverWind::Gameplay
