#include "ServerApplication.h"
#include "Network/GameplaySender.h"
#include <filesystem>
#include <iostream>
#include <stdexcept>
namespace EverWind
{
ServerApplication::~ServerApplication()
{
    Shutdown();
}
StartupPhase ServerApplication::Phase() const
{
    return phase;
}
bool ServerApplication::Initialize(const ServerConfig &settings)
{
    if (phase != StartupPhase::Created)
        return false;
    config = settings;
    try
    {
        InitializeIOCP();
        InitializeDatabase();
        InitializeGame();
        InitializeMaps();
        InitializeWorldObjects();
        ConnectCallbacks();
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
    network.sessions = std::make_unique<SessionManager>();
    network.server = std::make_unique<IOCPServer>();
    if (!network.server->Initialize(config.port, config.workers, network.sessions.get()))
        throw std::runtime_error("IOCP preparation failed");
    phase = StartupPhase::IOCP;
    std::cout << "[Initialize] IOCP prepared (not accepting connections)\n";
}
void ServerApplication::InitializeDatabase()
{
    db.connection = std::make_unique<DBManager>();
    if (!db.connection->Connect(config.dbHost, config.dbPort, config.dbUser, config.dbPassword,
                                config.dbSchema))
        throw std::runtime_error("Database connection failed");
    db.queries = std::make_unique<Queries>(*db.connection);
    db.persistence = std::make_shared<GameplayPersistence>(*db.connection);
    phase = StartupPhase::Database;
    std::cout << "[Initialize] Database connected\n";
}
std::string ServerApplication::ResolveMapsPath() const
{
    if (!config.mapsPath.empty())
        return config.mapsPath;
    char configured[32768]{};
    DWORD length = GetEnvironmentVariableA("EVERWIND_MAP_DIRECTORY", configured, sizeof configured);
    if (length > 0 && length < sizeof configured)
        return configured;
    wchar_t executable[32768]{};
    GetModuleFileNameW(nullptr, executable, 32768);
    auto bundled = std::filesystem::path(executable).parent_path() / "Shared" / "Maps";
    if (std::filesystem::exists(bundled))
        return bundled.string();
    return (std::filesystem::path(__FILE__).parent_path() / "Shared" / "Maps")
        .string();
}
void ServerApplication::InitializeGame()
{
    ingame.service = std::make_unique<EverWind::Gameplay::Service>();
    ingame.service->InitializeGame(db.queries->LoadGameDefinitions());
    db.persistence->Initialize(ingame.service->Rules().items);
    phase = StartupPhase::Game;
    std::cout << "[Initialize] Game rules and pipeline prepared\n";
}
void ServerApplication::InitializeMaps()
{
    ingame.service->InitializeMaps(ResolveMapsPath());
    // Only identities are registered in DB for the player's MapId foreign key; spatial data stays in files.
    if (!db.queries->EnsureMapIds(ingame.service->MapIds()))
        throw std::runtime_error("Map identity registration failed");
    phase = StartupPhase::Maps;
    std::cout << "[Initialize] Maps, terrain and spawn regions prepared\n";
}
void ServerApplication::InitializeWorldObjects()
{
    ingame.service->InitializeWorldObjects();
    phase = StartupPhase::WorldObjects;
    std::cout << "[Initialize] Monsters, resources, collision and triggers prepared\n";
}
void ServerApplication::ConnectCallbacks()
{
    ingame.authentication = std::make_unique<PacketMethod>(
        *db.queries, *network.sessions, [this](int id) { return ingame.service->HasMap(id); });
    network.server->SetAuthenticationInput(
        [this](Session &s, const NetPackets::PacketHeader &h, const char *p, size_t n) {
            return ingame.authentication->HandlePacket(&s, h, p, n);
        });
    network.server->SetGameplayInput([this](Session &session, uint16_t id, const char *payload, size_t size) {
        using namespace EverWind::Gameplay;
        ServiceWork work{};
        work.id = session.GetServerUserId();
        work.raw = true;
        work.packetId = id;
        work.authenticated = !session.GetUserId().empty();
        if (size && payload)
            work.payload.assign(payload, payload + size);
        std::weak_ptr<Session> weak = session.shared_from_this();
        work.send = GameplaySender::Create(weak);
        work.alive = [weak] {
            auto s = weak.lock();
            return s && !s->IsClosed();
        };
        work.close = [weak] {
            if (auto s = weak.lock())
                s->Close();
        };
        work.moveSpeed = session.GetGameplayMoveSpeed();
        work.save = [store = db.persistence, user = session.GetUserId()](const AccountState &state) {
            return store->Save(user, state);
        };
        work.load = [store = db.persistence, user = session.GetUserId()] { return store->Load(user); };
        ingame.service->Submit(std::move(work));
        return true;
    });
    phase = StartupPhase::Connected;
    std::cout << "[Initialize] Callbacks connected\n";
}
bool ServerApplication::Start()
{
    if (phase != StartupPhase::Connected)
        return false;
    try
    {
        ingame.service->Start();
        if (!network.server->Start())
            throw std::runtime_error("IOCP start failed");
        phase = StartupPhase::Running;
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
    if (phase == StartupPhase::Stopped)
        return;
    if (network.server)
        network.server->StopAccepting();
    if (ingame.service)
        ingame.service->Stop();
    if (network.server)
        network.server->Shutdown();
    ingame.service.reset();
    ingame.authentication.reset();
    network.server.reset();
    network.sessions.reset();
    db.persistence.reset();
    db.queries.reset();
    db.connection.reset();
    phase = StartupPhase::Stopped;
}

} // namespace EverWind
