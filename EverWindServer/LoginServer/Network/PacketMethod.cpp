#include "PacketMethod.h"
#include "GameplayPackets.h"
#include "../Validation/PacketValidator.h"
#include "../Validation/Validation.h"
#include <cstring>
#include <iostream>
#include <syncstream>
namespace
{
size_t SafeStringLength(const char *str, size_t maxLen)
{
    if (!str)
    {
        return 0;
    }

    size_t len = 0;

    while (len < maxLen && str[len] != '\0')
    {
        ++len;
    }

    return len;
}
}

PacketMethod::PacketMethod(std::shared_ptr<Queries> queries, SessionManager &connections, EverWind::Gameplay::MapManager &manager)
    : query(std::move(queries)), sessions(connections), maps(manager)
{
}

std::vector<char> PacketMethod::BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y, float z, int tutorialStep)
{
    NetPackets::PKT_LOGIN_ACK ack{resultCode, userDbId, mapId, x,
                                  y, z, tutorialStep};
    NetPackets::PacketHeader header;

    header.Length = static_cast<uint16_t>(sizeof(header) + sizeof(ack));
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_LOGIN_ACK);

    std::vector<char> buffer(header.Length);

    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

std::vector<char> PacketMethod::BuildSignUpAck(int resultCode)
{
    NetPackets::PKT_SIGNUP_ACK ack{};

    ack.ResultCode = resultCode;

    NetPackets::PacketHeader header{};

    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_SIGNUP_ACK);

    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);

    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    std::cout << "[Sign-up ack packet created] Result: " << resultCode << "\n";

    return buffer;
}

std::vector<char> PacketMethod::BuildStatAck(int atk, int def, float speed, float hp, float max_hp)
{
    NetPackets::PKT_USERSTAT ack{};

    ack.attack_power = atk;

    ack.defence_power = def;

    ack.speed = speed;

    ack.hp = hp;

    ack.max_hp = max_hp;

    ack.tutorialStep = 0;

    NetPackets::PacketHeader header{};

    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_PLAYERSTAT);

    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);

    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

std::vector<char>
PacketMethod::BuildInventoryAck(const GameStruct::InventoryItem &item)
{
    NetPackets::PKT_INVENTORY_ITEM ack{};

    std::memset(&ack, 0, sizeof(ack));

    size_t actualLen = SafeStringLength(item.ItemId, sizeof(item.ItemId));
    size_t limit = sizeof(ack.ItemId) - 1;
    size_t copyLen = (actualLen < limit) ? actualLen : limit;

    std::memcpy(ack.ItemId, item.ItemId, copyLen);

    ack.ItemId[copyLen] = '\0';

    ack.amount = item.amount;

    ack.slotIndex = item.slotIndex;

    ack.isEquipped = item.isEquipped;

    NetPackets::PacketHeader header{};

    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_INVENTORYITEM);

    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);

    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

std::vector<char>
PacketMethod::BuildQuestInfoAck(const QuestSaveData &questData)
{
    NetPackets::PKT_QUEST_DATA ack{};

    ack.questId = questData.questId;

    ack.isCompleted = questData.isCompleted ? 1 : 0;

    ack.rewardClaimed = questData.rewardClaimed ? 1 : 0;

    ack.conditionCount = (int)questData.currentCounts.size();

    int copyCount = ack.conditionCount;

    if (copyCount > NetPackets::MAX_QUEST_PROGRESS_COUNT)
    {
        copyCount = NetPackets::MAX_QUEST_PROGRESS_COUNT;

        ack.conditionCount = copyCount;
    }

    for (int i = 0; i < copyCount; ++i)
    {
        ack.currentCounts[i] = questData.currentCounts[i];
    }

    NetPackets::PacketHeader header{};

    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_QUEST_INFO);

    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);

    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

bool PacketMethod::HandleLoginRequest(Session *session, const NetPackets::PKT_LOGIN_REQ &packet)
{
    std::string userId(packet.UserID, SafeStringLength(packet.UserID, sizeof(packet.UserID)));
    std::string password(packet.Password, SafeStringLength(packet.Password, sizeof(packet.Password)));

    std::cout << "[Login request received] UserID: " << userId << "\n";

    if (userId.empty() || password.empty())
    {
        std::vector<char> failAck = BuildLoginAck(1, 0, 0, 0, 0, 0, 0);

        session->PostSend(failAck.data(), failAck.size());

        return false;
    }

    std::string dbUserId;

    int mapId = 0;

    float x = 0, y = 0, z = 0;

    int tutorialStep = 0;

    bool found = false;

    if (!query->FetchUser(userId, dbUserId, mapId, x, y, z, tutorialStep, found) || !found)
    {
        std::vector<char> failAck = BuildLoginAck(1, 0, 0, 0, 0, 0, 0);

        session->PostSend(failAck.data(), failAck.size());

        return true;
    }

    bool matches = false;

    if (!query->VerifyPassword(userId, password, matches) || !matches)
    {
        std::vector<char> failAck = BuildLoginAck(2, 0, 0, 0, 0, 0, 0);

        session->PostSend(failAck.data(), failAck.size());

        return true;
    }

    if (!sessions.TryAuthenticate(session, dbUserId))
    {
        std::vector<char> failAck = BuildLoginAck(3, 0, 0, 0, 0, 0, 0);

        session->PostSend(failAck.data(), failAck.size());

        return true;
    }

    session->SetMapId(mapId);

    session->SetPosition(x, y, z);

    if (!maps.HasMap(mapId))
    {
        auto ack = BuildLoginAck(4, 0, 0, 0, 0, 0, 0);

        session->PostSend(ack.data(), ack.size());

        return false;
    }
    // 맵 상태 스냅샷과 소속 등록은 서버 입장 요청에서 처리한다.
    int atk, def;

    float speed, hp, max_hp;

    if (query->FetchUserStat(userId, atk, def, speed, hp, max_hp))
    {
        session->SetGameplayMoveSpeed(speed);

        std::vector<char> statBuf = BuildStatAck(atk, def, speed, hp, max_hp);

        session->PostSend(statBuf.data(), statBuf.size());
    }

    std::vector<QuestSaveData> quests;

    if (query->FetchUserQuests(userId, quests))
    {
        for (const auto &quest : quests)
        {
            std::vector<char> questBuf = BuildQuestInfoAck(quest);

            session->PostSend(questBuf.data(), questBuf.size());
        }
    }

    std::vector<char> loginAckBuf = BuildLoginAck(0, session->GetServerUserId(), mapId, x, y, z, tutorialStep);

    session->PostSend(loginAckBuf.data(), loginAckBuf.size());

    std::vector<GameStruct::InventoryItem> items;

    if (query->FetchInventory(userId, items))
    {
        for (const auto &item : items)
        {
            std::vector<char> invBuf = BuildInventoryAck(item);

            session->PostSend(invBuf.data(), invBuf.size());
        }
    }

    return true;
}

bool PacketMethod::HandleSignUpRequest(Session *session, const NetPackets::PKT_SIGNUP_REQ &packet)
{
    std::string userId(packet.UserID, SafeStringLength(packet.UserID, sizeof(packet.UserID)));
    std::string password(packet.Password, SafeStringLength(packet.Password, sizeof(packet.Password)));

    std::cout << "[Sign-up request received] UserID: " << userId << "\n";

    if (userId.empty() || password.empty())
    {
        std::vector<char> failAck = BuildSignUpAck(2);

        session->PostSend(failAck.data(), failAck.size());

        return false;
    }

    bool duplicated = false;

    if (!query->InsertUser(userId, password, duplicated))
    {
        std::vector<char> errorAck = BuildSignUpAck(2);

        session->PostSend(errorAck.data(), errorAck.size());

        return true;
    }

    if (duplicated)
    {
        std::vector<char> dupAck = BuildSignUpAck(1);

        session->PostSend(dupAck.data(), dupAck.size());

        return true;
    }

    std::vector<char> successAck = BuildSignUpAck(0);

    session->PostSend(successAck.data(), successAck.size());

    return true;
}

bool PacketMethod::HandlePacket(Session *session, const NetPackets::PacketHeader &header, const char *payload, size_t size)
{
    using namespace EverWind::Gameplay;

    if (!session)
    {
        return false;
    }

    ValidationContext envelope;

    envelope.id = header.Id;

    envelope.payload = payload;

    envelope.size = size;

    envelope.authenticated = !session->GetUserId().empty();

    if (auto error = Validation::Chain().Then<PacketValidator>(PacketValidator::Envelope).Check(envelope); error != Error::None)
    {
        std::osyncstream(std::cout)
            << "[Packet Rejected] connection=" << session->GetServerUserId()
            << " packet=" << header.Id << " bytes=" << size
            << " reason=" << ErrorName(error) << '\n';
        return false;
    }

    if (RequestPayloadSize(header.Id))
    {
        Request request;

        if (!DecodeRequest(header.Id, payload, size, request))
        {
            return false;
        }

        if (Validation::Chain().Then<PacketValidator>().Check(ValidationContext::ForRequest(request)) != Error::None)
        {
            return maps.Enqueue(MakeGameWork(*session, request), nullptr);
        }

        switch (static_cast<NetPackets::PacketId>(header.Id))
        {
        case NetPackets::PacketId::C2S_JOIN_REQUEST:
            return HandleJoinPacket(*session, request);
        case NetPackets::PacketId::C2S_MOVE_REQUEST:
            return HandleMovePacket(*session, request);
        case NetPackets::PacketId::C2S_MODE_REQUEST:
            return HandleModePacket(*session, request);
        case NetPackets::PacketId::C2S_ATTACK_REQUEST:
            return HandleAttackPacket(*session, request);
        case NetPackets::PacketId::C2S_ATTACK_TARGETS_REQUEST:
            return HandleAttackTargetsPacket(*session, request);
        case NetPackets::PacketId::C2S_UPGRADE_STAT_REQUEST:
        case NetPackets::PacketId::C2S_UPGRADE_SKILL_REQUEST:
        case NetPackets::PacketId::C2S_RESET_STATS_REQUEST:
        case NetPackets::PacketId::C2S_RESET_SKILLS_REQUEST:
            return HandleGrowthPacket(*session, request);
        case NetPackets::PacketId::C2S_GATHER_REQUEST:
            return HandleGatherPacket(*session, request);
        case NetPackets::PacketId::C2S_CRAFT_REQUEST:
            return HandleCraftPacket(*session, request);
        case NetPackets::PacketId::C2S_CANCEL_REQUEST:
            return HandleCancelPacket(*session, request);
        case NetPackets::PacketId::C2S_CHANGE_MAP_REQUEST:
            return HandleChangeMapPacket(*session, request);
        case NetPackets::PacketId::C2S_REVIVE_REQUEST:
            return HandleRevivePacket(*session, request);
        case NetPackets::PacketId::C2S_PROBE_REQUEST:
            return HandleProbePacket(*session, request);
        case NetPackets::PacketId::C2S_USE_ITEM_REQUEST:
            return HandleUseItemPacket(*session, request);
        case NetPackets::PacketId::C2S_EQUIP_REQUEST:
            return HandleEquipPacket(*session, request);
        case NetPackets::PacketId::C2S_UNEQUIP_REQUEST:
            return HandleUnequipPacket(*session, request);
        case NetPackets::PacketId::C2S_CLAIM_QUEST_REQUEST:
            return HandleClaimQuestPacket(*session, request);
        case NetPackets::PacketId::C2S_RESOURCE_REFILL_REQUEST:
            return HandleResourceRefillPacket(*session, request);
        case NetPackets::PacketId::C2S_TUTORIAL_REQUEST:
            return HandleTutorialPacket(*session, request);
        case NetPackets::PacketId::C2S_SAVE_REQUEST:
            return HandleSavePacket(*session, request);
        case NetPackets::PacketId::C2S_LOGOUT_REQUEST:
            return HandleLogoutPacket(*session, request);
        }

        return false;
    }

    if (header.Id == static_cast<uint16_t>(NetPackets::PacketId::C2S_LOGIN_REQ))
    {
        NetPackets::PKT_LOGIN_REQ packet;

        std::memcpy(&packet, payload, sizeof packet);

        return HandleLoginRequest(session, packet);
    }

    if (header.Id == static_cast<uint16_t>(NetPackets::PacketId::C2S_SIGNUP_REQ))
    {
        NetPackets::PKT_SIGNUP_REQ packet;

        std::memcpy(&packet, payload, sizeof packet);

        return HandleSignUpRequest(session, packet);
    }

    return false;
}

EverWind::Gameplay::GameWork
PacketMethod::MakeGameWork(Session &session, const EverWind::Gameplay::Request &request)
{
    using namespace EverWind::Gameplay;

    GameWork work;

    work.id = session.GetServerUserId();

    work.request = request;

    work.authenticated = !session.GetUserId().empty();

    if (work.authenticated)
    {
        work.player = session.AcquirePlayer();
    }

    std::weak_ptr<Session> weak = session.shared_from_this();

    work.send = session.CreateGameSendCallback();

    work.alive = [weak]
    {
        auto s = weak.lock();

        return s && !s->IsClosed();
    };

    work.bindPlayer = [weak](std::shared_ptr<Player> p)
    {
        if (auto s = weak.lock())
        {
            s->SetPlayer(std::move(p));
        }
    };

    work.moveSpeed = session.GetGameplayMoveSpeed();

    work.save = [store = query,
                 user = session.GetUserId()](const AccountState &state)
    {
        return store->SaveGameplayState(user, state);
    };

    work.load = [store = query, user = session.GetUserId()]
    {
        return store->LoadGameplayState(user);
    };

    return work;
}

bool PacketMethod::HandleJoinPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleJoinRequest);
}

bool PacketMethod::HandleMovePacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleMoveRequest);
}

bool PacketMethod::HandleModePacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleModeRequest);
}

bool PacketMethod::HandleAttackPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleAttackRequest);
}

bool PacketMethod::HandleAttackTargetsPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleAttackTargetsRequest);
}

bool PacketMethod::HandleGrowthPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleGrowthRequest);
}

bool PacketMethod::HandleGatherPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleGatherRequest);
}

bool PacketMethod::HandleCraftPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleCraftRequest);
}

bool PacketMethod::HandleCancelPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleCancelRequest);
}

bool PacketMethod::HandleChangeMapPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleChangeMapRequest);
}

bool PacketMethod::HandleRevivePacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleReviveRequest);
}

bool PacketMethod::HandleProbePacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleProbeRequest);
}

bool PacketMethod::HandleUseItemPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleUseItemRequest);
}

bool PacketMethod::HandleEquipPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleEquipRequest);
}

bool PacketMethod::HandleUnequipPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleUnequipRequest);
}

bool PacketMethod::HandleClaimQuestPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleClaimQuestRequest);
}

bool PacketMethod::HandleResourceRefillPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleResourceRefillRequest);
}

bool PacketMethod::HandleTutorialPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleTutorialRequest);
}

bool PacketMethod::HandleSavePacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleSaveRequest);
}

bool PacketMethod::HandleLogoutPacket(Session &session, const EverWind::Gameplay::Request &request)
{
    return maps.Enqueue(MakeGameWork(session, request), &EverWind::Gameplay::GameplayProcessor::HandleLogoutRequest);
}
