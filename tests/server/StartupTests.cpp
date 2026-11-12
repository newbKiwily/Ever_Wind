#include "ServerApplication.h"
#include <cassert>
#include <iostream>
#include <condition_variable>
#include <mutex>
#include <chrono>
using namespace EverWind;
namespace {
uint16_t FreePort()
{
    SOCKET socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    assert(socket != INVALID_SOCKET);
    sockaddr_in address{}; address.sin_family = AF_INET;
    address.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    assert(bind(socket, reinterpret_cast<sockaddr *>(&address), sizeof address) == 0);
    int size = sizeof address;
    assert(getsockname(socket, reinterpret_cast<sockaddr *>(&address), &size) == 0);
    closesocket(socket);
    return ntohs(address.sin_port);
}
bool Connects(uint16_t port)
{
    SOCKET socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    sockaddr_in address{}; address.sin_family = AF_INET;
    address.sin_addr.s_addr = htonl(INADDR_LOOPBACK); address.sin_port = htons(port);
    bool connected = connect(socket, reinterpret_cast<sockaddr *>(&address), sizeof address) == 0;
    closesocket(socket);
    return connected;
}
}
int main()
{
    WSADATA data{}; assert(WSAStartup(MAKEWORD(2,2), &data) == 0);
    {
        // Confirm a receive completion before closing a connection with its next receive pending.
        SessionManager sessions; IOCPServer transport;
        auto port = FreePort();
        std::mutex mutex; std::condition_variable ready; bool received = false;
        assert(transport.Initialize(port, 2, &sessions));
        transport.SetAuthenticationInput([](Session &, const NetPackets::PacketHeader &, const char *, size_t) { return false; });
        transport.SetGameplayInput([&](Session &, uint16_t, const char *, size_t) {
            std::lock_guard<std::mutex> lock(mutex); received = true; ready.notify_all(); return true;
        });
        assert(transport.Start());
        SOCKET client = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        sockaddr_in address{}; address.sin_family = AF_INET;
        address.sin_addr.s_addr = htonl(INADDR_LOOPBACK); address.sin_port = htons(port);
        assert(connect(client, reinterpret_cast<sockaddr *>(&address), sizeof address) == 0);
        NetPackets::PacketHeader header{}; header.Length = sizeof header; header.Id = EverWind::Gameplay::RequestPacket;
        assert(send(client, reinterpret_cast<const char *>(&header), sizeof header, 0) == sizeof header);
        { std::unique_lock<std::mutex> lock(mutex); assert(ready.wait_for(lock, std::chrono::seconds(3), [&] { return received; })); }
        transport.Shutdown(); closesocket(client);
    }
    ServerConfig config; config.port = FreePort(); config.workers = 2;
    config.rulesPath = "Shared/Gameplay.rules";
    {
        EverWind::ServerApplication app;
        assert(app.Phase() == StartupPhase::Created && !app.Start());
        auto invalid = config; invalid.dbPort = 1;
        assert(!app.Initialize(invalid) && app.Phase() == StartupPhase::Stopped);
        // The socket prepared before the failed DB connection must be released.
        SessionManager sessions; IOCPServer transport;
        assert(transport.Initialize(config.port, 1, &sessions));
        assert(!Connects(config.port));
        transport.Shutdown(); transport.Shutdown();
    }
    {
        EverWind::ServerApplication app;
        auto invalid = config; invalid.rulesPath = "tests/server/not-present.rules";
        assert(!app.Initialize(invalid) && app.Phase() == StartupPhase::Stopped);
    }
    // Repeat on the same port, including shutdown with a pending receive.
    for (int iteration = 0; iteration < 2; ++iteration) {
        EverWind::ServerApplication app;
        assert(app.Initialize(config) && app.Phase() == StartupPhase::Connected);
        assert(!Connects(config.port));
        assert(app.Start() && app.Phase() == StartupPhase::Running);
        assert(!app.Start());
        SOCKET client = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        sockaddr_in address{}; address.sin_family = AF_INET;
        address.sin_addr.s_addr = htonl(INADDR_LOOPBACK); address.sin_port = htons(config.port);
        assert(connect(client, reinterpret_cast<sockaddr *>(&address), sizeof address) == 0);
        // StopAccepting joins the accept thread even when it was processing this connection.
        app.Shutdown(); app.Shutdown();
        closesocket(client);
        assert(app.Phase() == StartupPhase::Stopped && !app.Start());
    }
    WSACleanup();
    std::cout << "PASS: staged startup, pre-Start connection refusal, DB/rules failure cleanup, repeated shutdown and port reuse\n";
}
