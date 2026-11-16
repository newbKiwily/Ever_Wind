#pragma once
#include "../CoreData/WorldTypes.h"
namespace EverWind::Gameplay
{
class MapManager;
}
#include "../GameplayProcessor.h"
#include "../CoreData/GameDataHub.h"
#include <condition_variable>
#include <mutex>
#include <optional>
#include <random>
#include <sstream>
#include <thread>
namespace EverWind::Gameplay
{
struct RefillSystem;

class MapManager
{
  private:
    GameDataHub data;

    std::vector<std::unique_ptr<Map>> maps;

    std::unique_ptr<RefillSystem> refill;

    std::unique_ptr<GameplayProcessor> processor;

    std::mutex queueMutex;

    std::condition_variable queueReady;

    std::deque<GameWork> queue;

    bool stopping = false;

    std::unordered_map<int, std::function<bool()>> connections;

    std::unordered_map<int, std::function<void(std::shared_ptr<Player>)>> bindings;

    std::thread thread;

    bool started = false, stopped = false;
    bool initialized = false;

    std::vector<std::string> objectRows;

    std::string sourcePath;

    std::mt19937 random{std::random_device{}()};

    void InitializeGame(GameDataHub definitions);

    void InitializeMaps(const std::string &directory);

    void InitializeWorldObjects();

    void Run();

    void ParseMapRow(const std::string &line);

    bool ParseMapFeature(const std::string &type, std::istringstream &stream);
  public:
    using Handler = Error (GameplayProcessor::*)(Player &, const Request &);
    // 시뮬레이션 값은 게임 스레드와 해당 검증기 및 처리기에서만 사용한다.
    int nextPlacement = 100000000;

    double now = 0, nextSnapshot = 0;

    uint64_t nextAction = 1;

    MapManager &operator=(const MapManager &) = delete;

    MapManager();
    ~MapManager();
    MapManager(const MapManager &) = delete;

    void Initialize(GameDataHub definitions, const std::string &directory);

    const GameDataHub &Data() const;

    bool Ready() const;

    bool HasMap(int id) const;

    Map &GetMap(int id);
    const Map &GetMap(int id) const;

    const std::vector<std::unique_ptr<Map>> &Maps() const;

    std::vector<int> MapIds() const;

    std::shared_ptr<Player> FindPlayer(int id) const;

    std::vector<std::pair<int, std::shared_ptr<Player>>> Players() const;

    void AddPlayer(std::shared_ptr<Player> player, int mapId);

    void TransferPlayer(int playerId, int destination);

    void RemovePlayer(int id);

    std::optional<Vec3> MonsterSpawn(Map &map, float radius, int ignoreActor = 0);

    Actor *Target(int id, int map);

    bool InRange(const Actor &a, const Actor &b, float reach) const;

    void Start();

    void Stop();

    bool Enqueue(GameWork work, Handler handler);
};
}
