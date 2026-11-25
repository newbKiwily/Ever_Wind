#include "Session.h"
#include "IOCPServer.h"
#include "Packet.h"
#include "GameplayPackets.h"
#include "../DB/Queries.h"
#include "SessionManager.h"
#include <iostream>
#include <cstring>
#include <sstream>
#include <cmath>
using namespace NetPackets;

namespace
{
const char *GetPacketName(PacketId packetId)
{
    switch (packetId)
    {
    case PacketId::C2S_JOIN_REQUEST:
        return "C2S_JOIN_REQUEST";
    case PacketId::C2S_MOVE_REQUEST:
        return "C2S_MOVE_REQUEST";
    case PacketId::C2S_MODE_REQUEST:
        return "C2S_MODE_REQUEST";
    case PacketId::C2S_ATTACK_REQUEST:
        return "C2S_ATTACK_REQUEST";
    case PacketId::C2S_GATHER_REQUEST:
        return "C2S_GATHER_REQUEST";
    case PacketId::C2S_CRAFT_REQUEST:
        return "C2S_CRAFT_REQUEST";
    case PacketId::C2S_CANCEL_REQUEST:
        return "C2S_CANCEL_REQUEST";
    case PacketId::C2S_CHANGE_MAP_REQUEST:
        return "C2S_CHANGE_MAP_REQUEST";
    case PacketId::C2S_REVIVE_REQUEST:
        return "C2S_REVIVE_REQUEST";
    case PacketId::C2S_PROBE_REQUEST:
        return "C2S_PROBE_REQUEST";
    case PacketId::C2S_USE_ITEM_REQUEST:
        return "C2S_USE_ITEM_REQUEST";
    case PacketId::C2S_EQUIP_REQUEST:
        return "C2S_EQUIP_REQUEST";
    case PacketId::C2S_UNEQUIP_REQUEST:
        return "C2S_UNEQUIP_REQUEST";
    case PacketId::C2S_CLAIM_QUEST_REQUEST:
        return "C2S_CLAIM_QUEST_REQUEST";
    case PacketId::C2S_TUTORIAL_REQUEST:
        return "C2S_TUTORIAL_REQUEST";
    case PacketId::C2S_RESOURCE_REFILL_REQUEST:
        return "C2S_RESOURCE_REFILL_REQUEST";
    case PacketId::C2S_SAVE_REQUEST:
        return "C2S_SAVE_REQUEST";
    case PacketId::C2S_LOGOUT_REQUEST:
        return "C2S_LOGOUT_REQUEST";
    case PacketId::C2S_ATTACK_TARGETS_REQUEST:
        return "C2S_ATTACK_TARGETS_REQUEST";
    case PacketId::C2S_UPGRADE_STAT_REQUEST:
        return "C2S_UPGRADE_STAT_REQUEST";
    case PacketId::C2S_UPGRADE_SKILL_REQUEST:
        return "C2S_UPGRADE_SKILL_REQUEST";
    case PacketId::C2S_RESET_STATS_REQUEST:
        return "C2S_RESET_STATS_REQUEST";
    case PacketId::C2S_RESET_SKILLS_REQUEST:
        return "C2S_RESET_SKILLS_REQUEST";
    case PacketId::S2C_ACK:
        return "S2C_ACK";
    case PacketId::S2C_WELCOME:
        return "S2C_WELCOME";
    case PacketId::S2C_SPAWN:
        return "S2C_SPAWN";
    case PacketId::S2C_DESPAWN:
        return "S2C_DESPAWN";
    case PacketId::S2C_STATE:
        return "S2C_STATE";
    case PacketId::S2C_MOVE_RESULT:
        return "S2C_MOVE_RESULT";
    case PacketId::S2C_ACTION_STARTED:
        return "S2C_ACTION_STARTED";
    case PacketId::S2C_ACTION_ENDED:
        return "S2C_ACTION_ENDED";
    case PacketId::S2C_ACTION_CANCELLED:
        return "S2C_ACTION_CANCELLED";
    case PacketId::S2C_HIT:
        return "S2C_HIT";
    case PacketId::S2C_INVENTORY:
        return "S2C_INVENTORY";
    case PacketId::S2C_ZONE:
        return "S2C_ZONE";
    case PacketId::S2C_SNAPSHOT_END:
        return "S2C_SNAPSHOT_END";
    case PacketId::S2C_STATS:
        return "S2C_STATS";
    case PacketId::S2C_EQUIPMENT:
        return "S2C_EQUIPMENT";
    case PacketId::S2C_QUEST:
        return "S2C_QUEST";
    case PacketId::S2C_TUTORIAL:
        return "S2C_TUTORIAL";
    case PacketId::S2C_SAVED:
        return "S2C_SAVED";
    case PacketId::S2C_GROWTH:
        return "S2C_GROWTH";
    case PacketId::S2C_SKILL_RANK:
        return "S2C_SKILL_RANK";
    case PacketId::C2S_LOGIN_REQ:
        return "C2S_LOGIN_REQ";
    case PacketId::S2C_LOGIN_ACK:
        return "S2C_LOGIN_ACK";
    case PacketId::C2S_SIGNUP_REQ:
        return "C2S_SIGNUP_REQ";
    case PacketId::S2C_SIGNUP_ACK:
        return "S2C_SIGNUP_ACK";
    case PacketId::C2S_MOVESYNC_REQ:
        return "C2S_MOVESYNC_REQ";
    case PacketId::SC2_MOVESYNC_ACK:
        return "SC2_MOVESYNC_ACK";
    case PacketId::S2C_PLAYERLIST_ACK:
        return "S2C_PLAYERLIST_ACK";
    case PacketId::S2C_PLAYERLIST_RUNTIME:
        return "S2C_PLAYERLIST_RUNTIME";
    case PacketId::S2C_LOGOUT_ACK:
        return "S2C_LOGOUT_ACK";
    case PacketId::_INVENTORYITEM:
        return "_INVENTORYITEM";
    case PacketId::_PLAYERSTAT:
        return "_PLAYERSTAT";
    case PacketId::S2C_ENEMY_SPAWN:
        return "S2C_ENEMY_SPAWN";
    case PacketId::C2S_ATTACK_REQ:
        return "C2S_ATTACK_REQ";
    case PacketId::S2C_ENEMY_DAMAGED:
        return "S2C_ENEMY_DAMAGED";
    case PacketId::C2S_ENEMY_MOVE_SYNC:
        return "C2S_ENEMY_MOVE_SYNC";
    case PacketId::S2C_ENEMY_MOVE_SYNC:
        return "S2C_ENEMY_MOVE_SYNC";
    case PacketId::C2S_ENEMY_ATTACK_ANIM:
        return "C2S_ENEMY_ATTACK_ANIM";
    case PacketId::S2C_ENEMY_ATTACK_ANIM:
        return "S2C_ENEMY_ATTACK_ANIM";
    case PacketId::_ONESHOT_ANIM_SYNC:
        return "_ONESHOT_ANIM_SYNC";
    case PacketId::_INTERACT_SYNC:
        return "_INTERACT_SYNC";
    case PacketId::_DEAD_SYNC:
        return "_DEAD_SYNC";
    case PacketId::_COMBAT_STATE_SYNC:
        return "_COMBAT_STATE_SYNC";
    case PacketId::C2S_MAP_CHANGE_REQ:
        return "C2S_MAP_CHANGE_REQ";
    case PacketId::S2C_MAP_CHANGE_ACK:
        return "S2C_MAP_CHANGE_ACK";
    case PacketId::C2S_ENEMY_DEAD_REQ:
        return "C2S_ENEMY_DEAD_REQ";
    case PacketId::S2C_ENEMY_DEAD_ACK:
        return "S2C_ENEMY_DEAD_ACK";
    case PacketId::S2C_QUEST_INFO:
        return "S2C_QUEST_INFO";
    case PacketId::C2S_QUEST_RESET:
        return "C2S_QUEST_RESET";
    case PacketId::C2S_QUEST_SAVE:
        return "C2S_QUEST_SAVE";
    default:
        return "UNKNOWN_PACKET";
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

std::string BuildSessionLabel(const Session &session)
{
    std::ostringstream oss;

    const std::string &userId = session.GetUserId();

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

void LogPacketEvent(const Session &session, const char *direction, const PacketHeader &header)
{
    // 연속적으로 수신되는 상태 스냅샷의 로그를 제한하여 콘솔 출력 과부하를 방지한다.
    if (header.Id >= 0x701 && header.Id <= 0x714)
    {
        return;
    }

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

Session::Session(IOCPServer *server, SOCKET socket)
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

    auto *context = new IOContext(shared_from_this(), IOOperation::Recv);

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

bool Session::PostSend(const char *data, size_t len)
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
        const PacketHeader *header = reinterpret_cast<const PacketHeader *>(packet.data());

        LogPacketEvent(*this, "Send", *header);
    }

    sendQueue_.push(std::move(packet));

    if (sending_)
    {
        return true;
    }

    auto *context = new IOContext(shared_from_this(), IOOperation::Send);

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

void Session::OnRecvCompleted(IOContext *context, size_t bytesTransferred)
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

void Session::OnSendCompleted(IOContext *context, size_t bytesTransferred)
{
    std::unique_ptr<IOContext> completed(context);

    if (!completed)
    {
        return;
    }

    std::lock_guard<std::mutex> guard(sendMutex_);

    if (IsClosed())
    {
        sending_ = false;

        return;
    }

    if (bytesTransferred == 0 || bytesTransferred > completed->expectedBytes)
    {
        sending_ = false;

        Close();

        return;
    }

    if (bytesTransferred == completed->expectedBytes && sendQueue_.empty())
    {
        sending_ = false;

        return;
    }

    auto next = std::make_unique<IOContext>(shared_from_this(), IOOperation::Send);

    if (bytesTransferred < completed->expectedBytes)
    {
        // 프레임의 일부만 송신될 수 있으므로 나머지 송신을 마친 뒤 다음 패킷을 처리한다.
        next->expectedBytes = completed->expectedBytes - bytesTransferred;

        std::memcpy(next->buffer, completed->wsaBuf.buf + bytesTransferred, next->expectedBytes);
    }
    else
    {
        auto packet = std::move(sendQueue_.front());

        sendQueue_.pop();

        next->expectedBytes = packet.size();

        std::memcpy(next->buffer, packet.data(), packet.size());
    }

    next->wsaBuf.len = static_cast<ULONG>(next->expectedBytes);

    DWORD sent = 0;

    if (WSASend(socket_, &next->wsaBuf, 1, &sent, 0, &next->overlapped, nullptr) == SOCKET_ERROR && WSAGetLastError() != WSA_IO_PENDING)
    {
        sending_ = false;

        Close();

        return;
    }

    next.release(); // 대기 중인 입출력 문맥의 소유권은 완료 처리로 넘긴다.
}

void Session::Close()
{
    bool expected = false;

    if (closed_.compare_exchange_strong(expected, true))
    {
        // 게임 스레드가 맵 소속 제거와 서버 확정 상태 저장을 담당한다.
        server_->GetSessionManager()->RemoveSession(serverUserId);

        if (socket_ != INVALID_SOCKET)
        {
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
        const PacketHeader *header = reinterpret_cast<const PacketHeader *>(recvBuffer_.data() + offset);

        try
        {
            ValidatePacketLength(header->Length);
        }
        catch (const std::exception &)
        {
            return false;
        }

        if (recvBuffer_.size() - offset < header->Length)
        {
            break;
        }

        size_t payloadSize = header->Length - sizeof(PacketHeader);

        const char *payload = recvBuffer_.data() + offset + sizeof(PacketHeader);

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

bool Session::ProcessPacket(const PacketHeader &header, const char *payload, size_t payloadSize)
{
    // 이동 유효성은 게임 검증 후 결정되므로 거절 로그는 해당 위치에서 출력한다.
    const bool move = header.Id == static_cast<uint16_t>(PacketId::C2S_MOVE_REQUEST);

    if (!move)
    {
        LogPacketEvent(*this, "Recv", header);
    }

    return server_->OnPacketReceived(this, header, payload, payloadSize);
}

SOCKET Session::GetSocket() const
{
    return socket_;
}

bool Session::IsClosed() const
{
    return closed_.load();
}

void Session::SetMapId(int assigned_mapId)
{
    mapId = assigned_mapId;

    return;
}

void Session::SetPosition(float x, float y, float z)
{
    std::lock_guard<std::mutex> lock(positionMutex_);

    position.x = x;

    position.y = y;

    position.z = z;

    return;
}

GameStruct::Vector3 Session::GetPostion()
{
    std::lock_guard<std::mutex> lock(positionMutex_);

    return position;
}

IOCPServer *Session::GetServer() const
{
    return server_;
}

int Session::GetMapId() const
{
    return mapId;
}

void Session::SetUserId(const std::string &id)
{
    userId = id;
}

const std::string &Session::GetUserId() const
{
    return userId;
}

void Session::SetServerUserId(const int &id)
{
    serverUserId = id;
}

const int &Session::GetServerUserId() const
{
    return serverUserId;
}

void Session::SetGameplayMoveSpeed(float speed)
{
    if (std::isfinite(speed) && speed > 0)
    {
        gameplayMoveSpeed_.store(speed);
    }
}

float Session::GetGameplayMoveSpeed() const
{
    return gameplayMoveSpeed_.load();
}

void Session::SetPlayer(std::shared_ptr<EverWind::Gameplay::Player> player)
{
    std::lock_guard<std::mutex> lock(playerMutex_);

    player_ = std::move(player);
}

std::shared_ptr<EverWind::Gameplay::Player> Session::GetPlayer() const
{
    std::lock_guard<std::mutex> lock(playerMutex_);

    return player_;
}

std::shared_ptr<EverWind::Gameplay::Player> Session::AcquirePlayer()
{
    std::lock_guard<std::mutex> lock(playerMutex_);

    if (!player_)
    {
        player_ = std::make_shared<EverWind::Gameplay::Player>();

        player_->id = serverUserId;
    }

    return player_;
}

bool Session::SendGameplayEvent(const EverWind::Gameplay::Event &event)
{
    if (IsClosed())
    {
        return false;
    }

    const auto bytes = EverWind::Gameplay::EncodeEvent(event);

    return PostSend(bytes.data(), bytes.size());
}

EverWind::Gameplay::Send Session::CreateGameSendCallback()
{
    std::weak_ptr<Session> weak = weak_from_this();

    return [weak](const EverWind::Gameplay::Event &event)
    {
        if (auto session = weak.lock())
        {
            session->SendGameplayEvent(event);
        }
    };
}
