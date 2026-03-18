#pragma once

#include <winsock2.h>
#include <mswsock.h>
#include <windows.h>
#include <atomic>
#include <vector>
#include <mutex>
#include <queue>
#include <memory.h>
#include <string>
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Mswsock.lib")
#include "Structs.h"
namespace NetPackets
{
    struct PacketHeader;
    enum class PacketId : uint16_t;
}

class IOCPServer;

class Session : public std::enable_shared_from_this<Session>
{
public:
    static constexpr size_t MAX_IO_BUFFER = 2048;

    enum class IOOperation
    {
        Recv,
        Send,
    };

    struct IOContext
    {
        OVERLAPPED overlapped;
        WSABUF wsaBuf;
        IOOperation operation;
        std::shared_ptr<Session> owner;
        size_t expectedBytes;
        char buffer[MAX_IO_BUFFER];

        IOContext(std::shared_ptr<Session> o, IOOperation op);
    };
public:
    Session(IOCPServer* server, SOCKET socket);
    ~Session();

    Session(const Session&) = delete;
    Session& operator=(const Session&) = delete;

    SOCKET GetSocket() const { return socket_; }

    bool PostRecv();
    bool PostSend(const char* data, size_t len);

    void OnRecvCompleted(IOContext* context, size_t bytesTransferred);
    void OnSendCompleted(IOContext* context, size_t bytesTransferred);

    void Close();
    bool IsClosed() const { return closed_.load(); }
    void SetMapId(int assigned_mapId) { mapId = assigned_mapId; return; }
    void SetPosition(float x, float y, float z)
    {   
        std::lock_guard<std::mutex> lock(positionMutex_);
        position.x = x;
        position.y = y;
        position.z = z;
        return;
    }
    GameStruct::Vector3 GetPostion()
    {   
        std::lock_guard<std::mutex> lock(positionMutex_);
        return position;
    }
    IOCPServer* GetServer()
    {
        return server_;
    }
    int GetMapId()
    {
        return mapId;
    }
    void SetUserId(const std::string& id)
    { 
        userId = id;
    }
    const std::string& GetUserId() const 
    { 
        return userId;
    }
    void SetServerUserId(const int& id)
    {
        serverUserId = id;
    }
    const int& GetServerUserId()
    {
        return serverUserId;
    }
private:
    bool HandlePackets();
    bool ProcessPacket(const NetPackets::PacketHeader& header, const char* payload, size_t payloadSize);
private:

    std::string userId;
    int serverUserId;

    int mapId;
    GameStruct::Vector3 position;
    

    IOCPServer* server_;
    SOCKET socket_;
    std::atomic<bool> closed_;
    std::vector<char> recvBuffer_;
    std::mutex sendMutex_;
    std::mutex positionMutex_;
    std::queue<std::vector<char>> sendQueue_;
    bool sending_;
};

