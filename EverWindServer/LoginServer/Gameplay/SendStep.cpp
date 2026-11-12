#include "SendStep.h"
namespace EverWind::Gameplay {
void SendStep::OnOutput(const Send &destination, const Event &event) const {
  // Transport adapters perform serialization and socket operations, never game
  // rules.
  if (destination)
    destination(event);
}
void SendStep::OnInvalidPacket(const ServiceWork &work) const {
  if (work.close)
    work.close();
}
} // namespace EverWind::Gameplay
