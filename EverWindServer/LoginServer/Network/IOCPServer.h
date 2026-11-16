#pragma once
// 윈도우 소켓 확장 헤더는 이 포함 순서를 유지해야 한다.
#include <winsock2.h>
#include <windows.h>
#include <mswsock.h>
class IOCPServer;
#include "SessionManager.h"
#include "Session.h"
#include <functional>
#include <condition_variable>
#include "Packet.h"
#include <algorithm>
#include <atomic>
#include <memory>
#include <mutex>
#include <thread>
#include <unordered_map>
#include <unordered_set>
#include <vector>
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Mswsock.lib")
class IOCPServer
{
  private:
    SOCKET listenSocket_;

    HANDLE completionPort_;

    std::atomic<bool> running_;

    std::thread acceptThread_;

    std::vector<std::thread> workerThreads_;

    SessionManager *sessionManager_;

    size_t workerCount_ = 0;

    bool winsockReady_ = false, initialized_ = false;

    std::atomic<bool> accepting_{false};

    std::function<bool(Session &, const NetPackets::PacketHeader &, const char *, size_t)> onPacket_;

    uint16_t port_;

    std::mutex ioMutex_;

    std::condition_variable ioDrained_;

    size_t outstandingIO_ = 0;

    void AcceptLoop(SOCKET listener);

    void WorkerLoop();
  public:
    IOCPServer();
    ~IOCPServer();

    bool Initialize(uint16_t port, size_t workerCount, SessionManager *sessionManager);

    bool Start();

    void StopAccepting();

    void SetPacketInput(std::function<bool(Session &, const NetPackets::PacketHeader &, const char *, size_t)> callback);

    void Shutdown();

    bool OnPacketReceived(Session *session, const NetPackets::PacketHeader &header, const char *payload, size_t payloadSize);

    SessionManager *GetSessionManager();

    void BeginIO();

    void EndIO();
};
