#pragma once
#include "ServerSteps.h"
#include "Map/WorldState.h"
namespace EverWind::Gameplay
{
enum class GamePhase
{
    Created,
    Rules,
    Maps,
    WorldObjects
};
class ServerRuntime
{
  public:
    ServerRuntime();
    void InitializeGame(const std::string &path);
    void InitializeMaps(const std::vector<MapInitialInfo> &maps = {});
    void InitializeWorldObjects();
    const GameRules &Rules() const;
    bool Ready() const;
    bool HasMap(int id) const;
    ~ServerRuntime();
    ServerRuntime(const ServerRuntime &) = delete;
    ServerRuntime &operator=(const ServerRuntime &) = delete;
    void Connect(int id, Send send, float speed = 5, const AccountState &account = {},
                 std::function<bool(const AccountState &)> save = {});
    void SaveAll();
    void Disconnect(int id);
    void Handle(int id, const Request &request);
    void Advance(double dt);
    double Now() const;
    void Submit(ServiceWork work);
    bool Wait(std::deque<ServiceWork> &batch);
    void Dispatch(const ServiceWork &work);
    void StopReceiving();
    bool DecodeInput(ServiceWork &work);
    void RejectConnection(const ServiceWork &work);

  private:
    void ConnectSteps();
    GameRules rules;
    WorldState world;
    RefillSystem refill;
    ServerSteps steps;
    GamePhase phase = GamePhase::Created;
};
} // namespace EverWind::Gameplay
