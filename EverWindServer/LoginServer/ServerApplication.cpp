#include "ServerApplication.h"
#include <filesystem>
#include <iostream>
#include <stdexcept>
namespace EverWind
{
ServerApplication::~ServerApplication()
{
    Shutdown();
}

bool ServerApplication::Initialize(const ServerConfig &settings)
{
    if (initialized || stopped || server)
    {
        return false;
    }

    config = settings;

    try
    {
        InitializeIOCP();

        InitializeDatabase();

        InitializeGame();

        ConnectCallbacks();

        initialized = true;

        return true;
    }
    catch (const std::exception &e)
    {
        std::cerr << "[Server initialization failed] " << e.what() << '\n';

        Shutdown();

        return false;
    }
}

void ServerApplication::InitializeIOCP()
{
    sessions = std::make_unique<SessionManager>();
    server = std::make_unique<IOCPServer>();

    if (!server->Initialize(config.port, config.workers, sessions.get()))
    {
        throw std::runtime_error("IOCP preparation failed");
    }

    std::cout << "[Initialize] IOCP prepared (not accepting connections)\n";
}

void ServerApplication::InitializeDatabase()
{
    database = std::make_unique<DBManager>();

    if (!database->Connect(config.dbHost, config.dbPort, config.dbUser, config.dbPassword, config.dbSchema))
    {
        throw std::runtime_error("Database connection failed");
    }

    queries = std::make_shared<Queries>(*database);

    std::cout << "[Initialize] Database connected\n";
}

std::string ServerApplication::ResolveMapsPath() const
{
    if (!config.mapsPath.empty())
    {
        return config.mapsPath;
    }

    char configured[32768]{};

    DWORD length = GetEnvironmentVariableA("EVERWIND_MAP_DIRECTORY", configured, sizeof configured);

    if (length > 0 && length < sizeof configured)
    {
        return configured;
    }

    wchar_t executable[32768]{};

    GetModuleFileNameW(nullptr, executable, 32768);

    auto bundled = std::filesystem::path(executable).parent_path() / "MapData" / "Maps";

    if (std::filesystem::exists(bundled))
    {
        return bundled.string();
    }

    return (std::filesystem::path(__FILE__).parent_path() / "MapData" / "Maps")
        .string();
}

void ServerApplication::InitializeGame()
{
    maps = std::make_unique<EverWind::Gameplay::MapManager>();

    maps->Initialize(queries->LoadGameDefinitions(), ResolveMapsPath());

    queries->InitializeGameplayItems(maps->Data().items);

    if (!queries->EnsureMapIds(maps->MapIds()))
    {
        throw std::runtime_error("Map identity registration failed");
    }
}

void ServerApplication::ConnectCallbacks()
{
    packets = std::make_unique<PacketMethod>(queries, *sessions, *maps);

    server->SetPacketInput([this](Session &s, const NetPackets::PacketHeader &h, const char *p, size_t n)
    {
        return packets->HandlePacket(&s, h, p, n);
    });

    std::cout << "[Initialize] Callbacks connected\n";
}

bool ServerApplication::Start()
{
    if (!initialized || running || stopped)
    {
        return false;
    }

    try
    {
        maps->Start();

        if (!server->Start())
        {
            throw std::runtime_error("IOCP start failed");
        }

        running = true;

        return true;
    }
    catch (const std::exception &e)
    {
        std::cerr << "[Server start failed] " << e.what() << '\n';

        Shutdown();

        return false;
    }
}

void ServerApplication::Shutdown()
{
    if (stopped)
    {
        return;
    }

    if (server)
    {
        server->StopAccepting();
    }

    if (maps)
    {
        maps->Stop();
    }

    if (server)
    {
        server->Shutdown();
    }

    maps.reset();
    packets.reset();
    server.reset();
    sessions.reset();
    queries.reset();
    database.reset();

    initialized = running = false;

    stopped = true;
}
}
