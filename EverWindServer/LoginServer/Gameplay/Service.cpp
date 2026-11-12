#include "Service.h"
#include <chrono>
#include <stdexcept>

namespace EverWind::Gameplay
{
Service::Service() = default;
void Service::InitializeGame(GameRules definitions)
{
    runtime.InitializeGame(std::move(definitions));
}
void Service::InitializeMaps(const std::string &directory)
{
    runtime.InitializeMaps(directory);
}
void Service::InitializeWorldObjects()
{
    runtime.InitializeWorldObjects();
}
const GameRules &Service::Rules() const
{
    return runtime.Rules();
}
bool Service::HasMap(int id) const
{
    return runtime.HasMap(id);
}
std::vector<int> Service::MapIds() const
{
    return runtime.MapIds();
}
void Service::Start()
{
    if (!runtime.Ready() || started || stopped)
        throw std::logic_error("Service requires complete initialization and one Start");
    thread = std::thread([this] { Run(); });
    started = true;
}

void Service::Run()
{
    using Clock = std::chrono::steady_clock;
    auto previous = Clock::now();
    for (;;)
    {
        std::deque<ServiceWork> batch;
        if (!runtime.Wait(batch))
            break;
        // Flush disconnected owners before loading replacement sessions, including
        // immediate reconnects.
        for (auto it = connections.begin(); it != connections.end();)
            if (!it->second())
            {
                runtime.Disconnect(it->first);
                it = connections.erase(it);
            }
            else
                ++it;
        for (auto &w : batch)
        {
            if (!w.alive())
                continue;
            // Packet validation/parsing stays on the game thread and precedes
            // persistence access.
            if (!runtime.DecodeInput(w))
                continue;
            if (!connections.count(w.id))
            {
                auto state = w.load ? w.load() : w.account ? *w.account : AccountState{};
                if (w.load && !state.loaded)
                {
                    runtime.RejectConnection(w);
                    continue;
                }
                connections.emplace(w.id, w.alive);
                runtime.Connect(w.id, std::move(w.send), w.moveSpeed, state, std::move(w.save));
            }
            runtime.Dispatch(w);
        }
        for (auto it = connections.begin(); it != connections.end();)
            if (!it->second())
            {
                runtime.Disconnect(it->first);
                it = connections.erase(it);
            }
            else
                ++it;
        auto current = Clock::now();
        double dt = std::chrono::duration<double>(current - previous).count();
        previous = current;
        // Fixed bounded steps; input floods do not advance world time.
        while (dt > 0)
        {
            double step = std::min(dt, .02);
            runtime.Advance(step);
            dt -= step;
        }
    }
    runtime.SaveAll();
}
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
Service::~Service()
{
    Stop();
}
void Service::Submit(int id, Request r, Send send, std::function<bool()> alive, float moveSpeed,
                     std::shared_ptr<const AccountState> account,
                     std::function<bool(const AccountState &)> save, std::function<AccountState()> load)
{
    Submit(ServiceWork{id, r, std::move(send), std::move(alive), moveSpeed, std::move(account),
                       std::move(save), std::move(load)});
}
void Service::Submit(ServiceWork work)
{
    runtime.Submit(std::move(work));
}
void Service::Stop()
{
    stopped = true;
    runtime.StopReceiving();
    if (thread.joinable())
        thread.join();
}

} // namespace EverWind::Gameplay
