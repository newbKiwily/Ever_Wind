#pragma once
#include "Network/IOCPServer.h"
#include "DB/DBManager.h"
#include "DB/Queries.h"
#include "Gameplay/Map/MapManager.h"
#include "Network/PacketMethod.h"
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

class ServerApplication
{
  private:
    ServerConfig config;

    std::unique_ptr<DBManager> database;

    std::shared_ptr<Queries> queries;

    std::unique_ptr<SessionManager> sessions;

    std::unique_ptr<IOCPServer> server;

    std::unique_ptr<Gameplay::MapManager> maps;

    std::unique_ptr<PacketMethod> packets;

    bool initialized = false, running = false, stopped = false;

    void InitializeIOCP();

    void InitializeDatabase();

    void InitializeGame();

    void ConnectCallbacks();

    std::string ResolveMapsPath() const;
  public:
    ServerApplication() = default;
    ~ServerApplication();
    ServerApplication(const ServerApplication &) = delete;

    ServerApplication &operator=(const ServerApplication &) = delete;

    bool Initialize(const ServerConfig &config = {});

    bool Start();

    void Shutdown();
};
}
