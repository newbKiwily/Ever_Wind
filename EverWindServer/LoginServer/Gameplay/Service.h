#pragma once
#include "Protocol.h"
#include "ServerRuntime.h"

#include "WorldTypes.h"
#include <algorithm>
#include <deque>
#include <memory>
#include <string>
#include <thread>
#include <unordered_map>

namespace EverWind::Gameplay
{
class Service
{
  public:
    Service();
    void InitializeGame(const std::string &path);
    void InitializeMaps(const std::vector<MapInitialInfo> &maps = {});
    void InitializeWorldObjects();
    void Start();
    const GameRules &Rules() const;
    bool HasMap(int id) const;
    ~Service();
    void Submit(int connectionId, Request request, Send send, std::function<bool()> alive,
                float moveSpeed = 5.0f, std::shared_ptr<const AccountState> account = {},
                std::function<bool(const AccountState &)> save = {}, std::function<AccountState()> load = {});
    void Submit(ServiceWork work);
    void Stop();

  private:
    ServerRuntime runtime;
    std::unordered_map<int, std::function<bool()>> connections;
    std::thread thread;
    bool started = false, stopped = false;
    void Run();
};
} // namespace EverWind::Gameplay
