#pragma once
#include "GameplayProcessor.h"
#include "GameplayValidation.h"
#include "ReceiveStep.h"
#include "SendStep.h"
namespace EverWind::Gameplay {
// Distinct input contracts; no artificial common Step inheritance.
struct ServerSteps {
  ReceiveStep receive;
  GameplayValidation validation;
  GameplayProcessor processing;
  SendStep send;
  explicit ServerSteps(WorldState &world, RefillSystem &refill)
      : validation(world, refill), processing(world, refill) {}
};
} // namespace EverWind::Gameplay
