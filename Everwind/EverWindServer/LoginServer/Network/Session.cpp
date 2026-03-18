#include "Session.h"
#include "IOCPServer.h"
#include "Packet.h"
#include "../Logic/PacketMethod.h"
#include "../DB/Queries.h"
#include "MapDataManager.h"
#include "MapData.h"
#include "SessionManager.h"
#include <iostream>
#include <cstring>

using namespace NetPackets;


Session::IOContext::IOContext(std::shared_ptr<Session> o, IOOperation op)
    : operation(op), owner(std::move(o)), expectedBytes(0)
{
    memset(&overlapped, 0, sizeof(overlapped));
    wsaBuf.buf = buffer;
    wsaBuf.len = static_cast<ULONG>(MAX_IO_BUFFER);
}

Session::Session(IOCPServer* server, SOCKET socket)
    : server_(server), socket_(socket), closed_(false), sending_(false),userId("")
{
    recvBuffer_.reserve(4096);
}

Session::~Session()
{
    Close();
}

bool Session::PostRecv()
{
    if (IsClosed())
    {
        return false;
    }
 
    auto* context = new IOContext(shared_from_this(), IOOperation::Recv);
    DWORD flags = 0;
    DWORD bytes = 0;
    if (WSARecv(socket_, &context->wsaBuf, 1, &bytes, &flags, &context->overlapped, nullptr) == SOCKET_ERROR)
    {
        if (WSAGetLastError() != WSA_IO_PENDING)
        {
            delete context;
            Close();
            return false;
        }
    }
    return true;
}

bool Session::PostSend(const char* data, size_t len)
{
    if (len == 0 || len > MAX_IO_BUFFER)
    {
        return false;
    }

    std::lock_guard<std::mutex> guard(sendMutex_);
    if (IsClosed())
    {
        return false;
    }

    std::vector<char> packet(len);
    std::memcpy(packet.data(), data, len);
    sendQueue_.push(std::move(packet));

    if (sending_)
    {
        return true;
    }

    auto* context = new IOContext(shared_from_this(), IOOperation::Send);
    auto nextPacket = std::move(sendQueue_.front());
    std::memcpy(context->buffer, nextPacket.data(), nextPacket.size());
    context->wsaBuf.len = static_cast<ULONG>(nextPacket.size());
    context->expectedBytes = nextPacket.size();
    sendQueue_.pop();
    sending_ = true;

    DWORD bytesSent = 0;
    if (WSASend(socket_, &context->wsaBuf, 1, &bytesSent, 0, &context->overlapped, nullptr) == SOCKET_ERROR)
    {
        if (WSAGetLastError() != WSA_IO_PENDING)
        {
            delete context;
            sending_ = false;
            Close();
            return false;
        }
    }

    return true;
}

void Session::OnRecvCompleted(IOContext* context, size_t bytesTransferred)
{
    if (context == nullptr)
    {
        return;
    }

    if (bytesTransferred == 0)
    {
        delete context;
        Close();
        return;
    }

    recvBuffer_.insert(recvBuffer_.end(), context->buffer, context->buffer + bytesTransferred);
    delete context;

    if (!HandlePackets())
    {
        Close();
        return;
    }

    if (!IsClosed())
    {
        PostRecv();
    }
}

void Session::OnSendCompleted(IOContext* context, size_t /*bytesTransferred*/)
{
    if (context == nullptr)
    {
        return;
    }

    delete context;

    std::lock_guard<std::mutex> guard(sendMutex_);
    if (sendQueue_.empty())
    {
        sending_ = false;
        return;
    }

    auto* newContext = new IOContext(shared_from_this(), IOOperation::Send);
    auto nextPacket = std::move(sendQueue_.front());
    std::memcpy(newContext->buffer, nextPacket.data(), nextPacket.size());
    newContext->wsaBuf.len = static_cast<ULONG>(nextPacket.size());
    newContext->expectedBytes = nextPacket.size();
    sendQueue_.pop();

    DWORD bytesSent = 0;
    if (WSASend(socket_, &newContext->wsaBuf, 1, &bytesSent, 0, &newContext->overlapped, nullptr) == SOCKET_ERROR)
    {
        if (WSAGetLastError() != WSA_IO_PENDING)
        {
            delete newContext;
            sending_ = false;
            Close();
            return;
        }
    }
}

void Session::Close()
{
    bool expected = false;
    if (closed_.compare_exchange_strong(expected, true))
    {
        if (!userId.empty()) {

            // 1. 맵에서 나를 제거 (추가됨)
            auto mapMgr = server_->GetSessionManager()->GetMapDataManager();
            if (auto currMap = mapMgr->findMapData(this->mapId)) {
                currMap->RemoveSession(shared_from_this());
            }
            server_->GetPacketMethod()->SendPlayerLogOut(this, serverUserId);
            

            // 3. 세션 매니저 제거 및 DB 저장
            server_->GetSessionManager()->RemoveSession(serverUserId);
            server_->GetPacketMethod()->getQuery()->UpdateUserPosition(userId, mapId, position.x, position.y, position.z);
            
        }

        if (socket_ != INVALID_SOCKET) {
            shutdown(socket_, SD_BOTH);
            closesocket(socket_);
            socket_ = INVALID_SOCKET;
        }
    }
}


bool Session::HandlePackets()
{
    size_t offset = 0;
    while (recvBuffer_.size() - offset >= sizeof(PacketHeader))
    {
        const PacketHeader* header = reinterpret_cast<const PacketHeader*>(recvBuffer_.data() + offset);
        try
        {
            ValidatePacketLength(header->Length);
        }
        catch (const std::exception&)
        {
            return false;
        }

        if (recvBuffer_.size() - offset < header->Length)
        {
            break;
        }

        size_t payloadSize = header->Length - sizeof(PacketHeader);
        const char* payload = recvBuffer_.data() + offset + sizeof(PacketHeader);
        if (!ProcessPacket(*header, payload, payloadSize))
        {
            return false;
        }
        offset += header->Length;
    }

    if (offset > 0)
    {
        recvBuffer_.erase(recvBuffer_.begin(), recvBuffer_.begin() + offset);
    }

    return true;
}

bool Session::ProcessPacket(const PacketHeader& header, const char* payload, size_t payloadSize)
{
    return server_->OnPacketReceived(this, header, payload, payloadSize);
}

