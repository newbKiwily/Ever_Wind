#pragma once
#include "WorldTypes.h"
#include <condition_variable>
#include <mutex>
namespace EverWind::Gameplay {
// Owns the IO-worker -> game-thread queue. Packet bytes are copied before
// submission.
class ReceiveStep {
public:
  void SetNext(std::function<void(const ServiceWork &)> callback);
  void Submit(ServiceWork work);
  bool Wait(std::deque<ServiceWork> &batch);
  void Dispatch(const ServiceWork &work) const;
  void Stop();

private:
  std::function<void(const ServiceWork &)> next;
  std::mutex mutex;
  std::condition_variable ready;
  std::deque<ServiceWork> queue;
  bool stopping = false;
};
} // namespace EverWind::Gameplay
