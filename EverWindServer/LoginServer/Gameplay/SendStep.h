#pragma once
#include "WorldTypes.h"
namespace EverWind::Gameplay {
class SendStep {
public:
  void OnOutput(const Send &destination, const Event &event) const;
  void OnInvalidPacket(const ServiceWork &work) const;
};
} // namespace EverWind::Gameplay
