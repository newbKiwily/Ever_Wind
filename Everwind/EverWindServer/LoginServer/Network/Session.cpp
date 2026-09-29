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
#include <sstream>

using namespace NetPackets;

namespace
{
    const char* GetPacketName(PacketId packetId)
    {
        switch (packetId)
        {
        case PacketId::C2S_LOGIN_REQ: return "C2S_LOGIN_REQ";
        case PacketId::S2C_LOGIN_ACK: return "S2C_LOGIN_ACK";
        case PacketId::C2S_SIGNUP_REQ: return "C2S_SIGNUP_REQ";
        case PacketId::S2C_SIGNUP_ACK: return "S2C_SIGNUP_ACK";
        case PacketId::C2S_MOVESYNC_REQ: return "C2S_MOVESYNC_REQ";
        case PacketId::SC2_MOVESYNC_ACK: return "SC2_MOVESYNC_ACK";
        case PacketId::S2C_PLAYERLIST_ACK: return "S2C_PLAYERLIST_ACK";
        case PacketId::S2C_PLAYERLIST_RUNTIME: return "S2C_PLAYERLIST_RUNTIME";
        case PacketId::S2C_LOGOUT_ACK: return "S2C_LOGOUT_ACK";
        case PacketId::_INVENTORYITEM: return "_INVENTORYITEM";
        case PacketId::_PLAYERSTAT: return "_PLAYERSTAT";
        case PacketId::S2C_ENEMY_SPAWN: return "S2C_ENEMY_SPAWN";
        case PacketId::C2S_ATTACK_REQ: return "C2S_ATTACK_REQ";
        case PacketId::S2C_ENEMY_DAMAGED: return "S2C_ENEMY_DAMAGED";
        case PacketId::C2S_ENEMY_MOVE_SYNC: return "C2S_ENEMY_MOVE_SYNC";
        case PacketId::S2C_ENEMY_MOVE_SYNC: return "S2C_ENEMY_MOVE_SYNC";
        case PacketId::C2S_ENEMY_ATTACK_ANIM: return "C2S_ENEMY_ATTACK_ANIM";
        case PacketId::S2C_ENEMY_ATTACK_ANIM: return "S2C_ENEMY_ATTACK_ANIM";
        case PacketId::_ONESHOT_ANIM_SYNC: return "_ONESHOT_ANIM_SYNC";
        case PacketId::_INTERACT_SYNC: return "_INTERACT_SYNC";
        case PacketId::_DEAD_SYNC: return "_DEAD_SYNC";
        case PacketId::_COMBAT_STATE_SYNC: return "_COMBAT_STATE_SYNC";
        case PacketId::C2S_MAP_CHANGE_REQ: return "C2S_MAP_CHANGE_REQ";
        case PacketId::S2C_MAP_CHANGE_ACK: return "S2C_MAP_CHANGE_ACK";
        case PacketId::C2S_ENEMY_DEAD_REQ: return "C2S_ENEMY_DEAD_REQ";
        case PacketId::S2C_ENEMY_DEAD_ACK: return "S2C_ENEMY_DEAD_ACK";
        case PacketId::S2C_QUEST_INFO: return "S2C_QUEST_INFO";
        case PacketId::C2S_QUEST_RESET: return "C2S_QUEST_RESET";
        case PacketId::C2S_QUEST_SAVE: return "C2S_QUEST_SAVE";
        default: return "UNKNOWN_PACKET";
        }
    }

    bool ShouldLogPacket(PacketId packetId)
    {
        switch (packetId)
        {
        case PacketId::C2S_MOVESYNC_REQ:
        case PacketId::SC2_MOVESYNC_ACK:
        case PacketId::C2S_ENEMY_MOVE_SYNC:
        case PacketId::S2C_ENEMY_MOVE_SYNC:
            return false;
        default:
            return true;
        }
    }

    std::string BuildSessionLabel(const Session& session)
    {
        std::ostringstream oss;
        const std::string& userId = session.GetUserId();

        oss << "User ";
        if (userId.empty())
        {
            oss << "'Anonymous'";
        }
        else
        {
            oss << "'" << userId << "'";
        }

        oss << " [ServerUserId: " << session.GetServerUserId()
            << ", MapId: " << session.GetMapId() << "]";
        return oss.str();
    }

    void LogPacketEvent(const Session& session, const char* direction, const PacketHeader& header)
    {
        const PacketId packetId = static_cast<PacketId>(header.Id);
        if (!ShouldLogPacket(packetId))
        {
            return;
        }

        std::cout << "[Packet " << direction << "] "
            << BuildSessionLabel(session)
            << " - " << GetPacketName(packetId)
            << " (0x" << std::hex << header.Id << std::dec
            << ", " << header.Length << " bytes)"
            << std::endl;
    }
}


Session::IOContext::IOContext(std::shared_ptr<Session> o, IOOperation op)
    : operation(op), owner(std::move(o)), expectedBytes(0)
{
    memset(&overlapped, 0, sizeof(overlapped));
    wsaBuf.buf = buffer;
    wsaBuf.len = static_cast<ULONG>(MAX_IO_BUFFER);
}

Session::Session(IOCPServer* server, SOCKET socket)
    : userId(""), serverUserId(-1), mapId(0), position{}, server_(server), socket_(socket), closed_(false), sending_(false)
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
    if (len >= sizeof(PacketHeader))
    {
        const PacketHeader* header = reinterpret_cast<const PacketHeader*>(packet.data());
        LogPacketEvent(*this, "Send", *header);
    }
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
    LogPacketEvent(*this, "Recv", header);
    return server_->OnPacketReceived(this, header, payload, payloadSize);
}


