#pragma once
#include "Network/IOCPServer.h"
#include "DB/DBManager.h"
#include "DB/Queries.h"
#include "DB/GameplayPersistence.h"
#include "Gameplay/Service.h"
#include "Gameplay/Authentication/PacketMethod.h"
namespace EverWind
{
struct ServerConfig
{
    std::string dbHost = "127.0.0.1";
    unsigned int dbPort = 3306;
    std::string dbUser = "SM";
    std::string dbPassword = "bitnami";
    std::string dbSchema = "GameServer";
    uint16_t port = 4000;
    size_t workers = 0;
    std::string mapsPath;
};
struct DatabaseComponents
{
    std::unique_ptr<DBManager> connection;
    std::unique_ptr<Queries> queries;
    std::shared_ptr<GameplayPersistence> persistence;
};
struct NetworkComponents
{
    std::unique_ptr<SessionManager> sessions;
    std::unique_ptr<IOCPServer> server;
};
struct IngameComponents
{
    std::unique_ptr<EverWind::Gameplay::Service> service;
    std::unique_ptr<PacketMethod> authentication;
};
enum class StartupPhase
{
    Created,
    IOCP,
    Database,
    Game,
    Maps,
    WorldObjects,
    Connected,
    Running,
    Stopped
};
class ServerApplication
{
  public:
    ServerApplication() = default;
    ~ServerApplication();
    ServerApplication(const ServerApplication &) = delete;
    ServerApplication &operator=(const ServerApplication &) = delete;
    bool Initialize(const ServerConfig &config = {});
    bool Start();
    void Shutdown();
    StartupPhase Phase() const;

  private:
    void InitializeIOCP();
    void InitializeDatabase();
    void InitializeGame();
    void InitializeMaps();
    void InitializeWorldObjects();
    void ConnectCallbacks();
    std::string ResolveMapsPath() const;
    ServerConfig config;
    DatabaseComponents db;
    NetworkComponents network;
    IngameComponents ingame;
    StartupPhase phase = StartupPhase::Created;
};

} // namespace EverWind
