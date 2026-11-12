#include "ReceiveStep.h"
#include <chrono>
namespace EverWind::Gameplay {
void ReceiveStep::SetNext(std::function<void(const ServiceWork &)> callback) {
  next = std::move(callback);
}
void ReceiveStep::Submit(ServiceWork work) {
  std::lock_guard<std::mutex> lock(mutex);
  if (stopping || queue.size() >= 4096)
    return;
  queue.push_back(std::move(work));
  ready.notify_one();
}
bool ReceiveStep::Wait(std::deque<ServiceWork> &batch) {
  std::unique_lock<std::mutex> lock(mutex);
  ready.wait_for(lock, std::chrono::milliseconds(10),
                 [&] { return stopping || !queue.empty(); });
  if (stopping)
    return false;
  batch.swap(queue);
  return true;
}
void ReceiveStep::Dispatch(const ServiceWork &work) const {
  if (next)
    next(work);
}
void ReceiveStep::Stop() {
  {
    std::lock_guard<std::mutex> lock(mutex);
    stopping = true;
  }
  ready.notify_one();
}
} // namespace EverWind::Gameplay
