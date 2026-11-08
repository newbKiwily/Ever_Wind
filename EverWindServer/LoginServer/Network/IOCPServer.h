#pragma once

#include <winsock2.h>
#include <windows.h>
#include <mswsock.h>
#include <vector>
#include <thread>
#include <atomic>
#include <mutex>
#include <unordered_map>
#include <unordered_set>
#include <memory>
#include <algorithm>
#include "Packet.h"

#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Mswsock.lib")

class PacketMethod;
class SessionManager;
class Session;
class IOCPServer
{
public:
    IOCPServer();
    ~IOCPServer();

    bool Initialize(uint16_t port, size_t workerCount,PacketMethod* packetMethod,SessionManager* sessionManager);
    void Shutdown();

    bool OnPacketReceived(Session* session, const NetPackets::PacketHeader& header, const char* payload, size_t payloadSize);
   
    PacketMethod* GetPacketMethod()
    {
        return loginLogic_;
    }

    SessionManager* GetSessionManager() 
    { 
        return sessionManager_;
    }

private:
    void AcceptLoop();
    void WorkerLoop();

private:
    SOCKET listenSocket_;
    HANDLE completionPort_;
    std::atomic<bool> running_;
    std::thread acceptThread_;
    std::vector<std::thread> workerThreads_;
    SessionManager* sessionManager_;
    PacketMethod* loginLogic_;
    uint16_t port_;
    std::mutex loginUserMutex_;
};

