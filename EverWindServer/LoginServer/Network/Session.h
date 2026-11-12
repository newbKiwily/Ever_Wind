#pragma once
class Session;
#include "IOCPServer.h"

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
#include "Packet.h"
#include "IOContext.h"

class Session : public std::enable_shared_from_this<Session>
{
public:
    static constexpr size_t MAX_IO_BUFFER = NetworkMaxIoBuffer;
    using IOOperation = ::IOOperation;
    using IOContext = ::IOContext;

public:
    Session(IOCPServer* server, SOCKET socket);
    ~Session();

    Session(const Session&) = delete;
    Session& operator=(const Session&) = delete;

    SOCKET GetSocket() const;

    bool PostRecv();
    bool PostSend(const char* data, size_t len);

    void OnRecvCompleted(IOContext* context, size_t bytesTransferred);
    void OnSendCompleted(IOContext* context, size_t bytesTransferred);

    void Close();
    bool IsClosed() const;
    void SetMapId(int assigned_mapId);
    void SetPosition(float x, float y, float z);
    GameStruct::Vector3 GetPostion();
    IOCPServer* GetServer() const;
    int GetMapId() const;
    void SetUserId(const std::string& id);
    const std::string& GetUserId() const;
    void SetServerUserId(const int& id);
    const int& GetServerUserId() const;
    void SetGameplayMoveSpeed(float speed);
    float GetGameplayMoveSpeed() const;
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
    std::atomic<float> gameplayMoveSpeed_{5.0f};
};
