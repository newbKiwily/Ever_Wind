#include <iostream>
#include <string>
#include <csignal>
#include <atomic>
#include "SessionManager.h"
#include "Network/IOCPServer.h"
#include "Logic/PacketMethod.h"
#include "DB/DBManager.h"
#include "../LoginServer/DB/Queries.h"
#include "MapData.h"
#include "TimerManager.h"
namespace
{
    std::atomic<bool> g_shouldStop = false;

    void SignalHandler(int)
    {
        g_shouldStop.store(true);
    }
}

int main()
{
    signal(SIGINT, SignalHandler);
    signal(SIGTERM, SignalHandler);

    const std::string dbHost = "127.0.0.1";
    const unsigned int dbPort = 3306;
    const std::string dbUser = "SM";
    const std::string dbPassword = "bitnami";
    const std::string dbSchema = "GameServer";            //DB이름넣기

    DBManager dbManager;
    if (!dbManager.Connect(dbHost, dbPort, dbUser, dbPassword, dbSchema))
    {
        std::cerr << "Failed to connect to MySQL server." << std::endl;
        return -1;
    }

    IOCPServer* server=new IOCPServer;
    PacketMethod* loginLogic=new PacketMethod(dbManager,server);
 

    SessionManager* sessionManager = new SessionManager(loginLogic);
    std::vector<MapInitialInfo> allMapConfigs;
    if (loginLogic->getQuery()->FetchAllMapData(allMapConfigs))
    {

        for (const auto& config : allMapConfigs)
        {
            MapData* newMap = new MapData(
                config.radius,
                config.enemySpawnX,
                config.enemySpawnY,
                config.enemySpawnZ,
                config.playerSpawnX,
                config.playerSpawnY,
                config.playerSpawnZ,
                config.maxEnemyCount,
                config.enemyIdList 
            );
            for (int i = 0; i < config.maxEnemyCount; ++i) {
                newMap->InstanceEnemy();
            }
            sessionManager->GetMapDataManager()->RegisterMap(config.mapId, newMap);
            std::cout << "Map ID " << config.mapId << " registered." << std::endl;
        }
    }
    const uint16_t listenPort = 4000;
    if (!server->Initialize(listenPort, 0,loginLogic,sessionManager))
    {
        std::cerr << "Failed to initialize IOCP server." << std::endl;
        return -1;
    }

    TimerManager* timerManager = new TimerManager;
    timerManager->Start();
    timerManager->AddRepeat(std::chrono::seconds(10), [sessionManager]()
    {
        sessionManager->GetMapDataManager()->RefillEnemyAllMap();
    });

    std::cout << "Login Server started on port " << listenPort << ". Type 'quit' to stop." << std::endl;

    std::string command;
    while (!g_shouldStop.load())
    {
        if (!std::getline(std::cin, command))
        {
            break;
        }
        if (command == "quit" || command == "exit")
        {
            break;
        }
    }

    server->Shutdown();
    delete server;
    std::cout << "Server stopped." << std::endl;
    return 0;
}

