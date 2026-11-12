#include "ServerRuntime.h"
#include <stdexcept>
namespace EverWind::Gameplay
{
ServerRuntime::ServerRuntime() : world(rules), refill(world), steps(world, refill)
{
}
void ServerRuntime::InitializeGame(GameRules definitions)
{
    if (phase != GamePhase::Created)
        throw std::logic_error("Game initialization out of order");
    definitions.Validate();
    rules = std::move(definitions);
    ConnectSteps();
    phase = GamePhase::Rules;
}
void ServerRuntime::InitializeMaps(const std::string &directory)
{
    if (phase != GamePhase::Rules)
        throw std::logic_error("Map initialization out of order");
    world.InitializeMaps(directory);
    phase = GamePhase::Maps;
}
void ServerRuntime::InitializeWorldObjects()
{
    if (phase != GamePhase::Maps)
        throw std::logic_error("World object initialization out of order");
    world.InitializeWorldObjects();
    phase = GamePhase::WorldObjects;
}
const GameRules &ServerRuntime::Rules() const
{
    return rules;
}
bool ServerRuntime::Ready() const
{
    return phase == GamePhase::WorldObjects;
}
bool ServerRuntime::HasMap(int id) const
{
    return world.maps.count(id) != 0;
}
std::vector<int> ServerRuntime::MapIds() const
{
    std::vector<int> ids;
    for (const auto &[id, map] : world.maps)
        ids.push_back(id);
    return ids;
}
ServerRuntime::~ServerRuntime()
{
    SaveAll();
}
void ServerRuntime::ConnectSteps()
{
    steps.receive.SetNext([this](const ServiceWork &work) { steps.validation.OnReceived(work); });
    steps.validation.SetNext(
        [this](const ValidationResult &result) { steps.processing.OnValidated(result); });
    steps.validation.SetInvalidPacket([this](const ServiceWork &work) { steps.send.OnInvalidPacket(work); });
    steps.processing.SetOutput(
        [this](const Send &destination, const Event &event) { steps.send.OnOutput(destination, event); });
}
void ServerRuntime::Connect(int id, Send send, float speed, const AccountState &account,
                            std::function<bool(const AccountState &)> save)
{
    steps.processing.Connect(id, std::move(send), speed, account, std::move(save));
}
void ServerRuntime::SaveAll()
{
    steps.processing.SaveAll();
}
void ServerRuntime::Disconnect(int id)
{
    steps.processing.Disconnect(id);
}
void ServerRuntime::Handle(int id, const Request &request)
{
    if (!Ready())
        throw std::logic_error("Game is not initialized");
    ServiceWork work{};
    work.id = id;
    work.request = request;
    Dispatch(work);
}
void ServerRuntime::Advance(double dt)
{
    steps.processing.Advance(dt);
}
double ServerRuntime::Now() const
{
    return world.now;
}
void ServerRuntime::Submit(ServiceWork work)
{
    steps.receive.Submit(std::move(work));
}
bool ServerRuntime::Wait(std::deque<ServiceWork> &batch)
{
    return steps.receive.Wait(batch);
}
void ServerRuntime::Dispatch(const ServiceWork &work)
{
    steps.receive.Dispatch(work);
}
void ServerRuntime::StopReceiving()
{
    steps.receive.Stop();
}
bool ServerRuntime::DecodeInput(ServiceWork &work)
{
    return steps.validation.DecodeInput(work);
}
void ServerRuntime::RejectConnection(const ServiceWork &work)
{
    steps.processing.RejectConnection(work);
}
} // namespace EverWind::Gameplay
