#include "PacketMethod.h"
#include <cstring>
#include <iostream>
namespace

{
size_t SafeStringLength(const char *str, size_t maxLen)
{
    if (!str)
        return 0;
    size_t len = 0;

    while (len < maxLen && str[len] != '\0')
        ++len;

    return len;
}

} // namespace

PacketMethod::PacketMethod(Queries &queries, SessionManager &connections, std::function<bool(int)> exists)
    : query(queries), sessions(connections), mapExists(std::move(exists))
{
}
std::vector<char> PacketMethod::BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y,
                                              float z, int tutorialStep)
{
    NetPackets::PKT_LOGIN_ACK ack{resultCode, userDbId, mapId, x, y, z, tutorialStep};

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
std::vector<char> PacketMethod::BuildInventoryAck(const GameStruct::InventoryItem &item)
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
std::vector<char> PacketMethod::BuildQuestInfoAck(const QuestSaveData &questData)
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

    if (!query.FetchUser(userId, dbUserId, mapId, x, y, z, tutorialStep, found) || !found)
    {
        std::vector<char> failAck = BuildLoginAck(1, 0, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return true;
    }

    bool matches = false;
    if (!query.VerifyPassword(userId, password, matches) || !matches)
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

    if (!mapExists || !mapExists(mapId))
    {
        auto ack = BuildLoginAck(4, 0, 0, 0, 0, 0, 0);
        session->PostSend(ack.data(), ack.size());
        return false;
    }
    // World snapshots and membership are handled by the authoritative Join request.
    int atk, def;
    float speed, hp, max_hp;
    if (query.FetchUserStat(userId, atk, def, speed, hp, max_hp))
    {
        session->SetGameplayMoveSpeed(speed);
        std::vector<char> statBuf = BuildStatAck(atk, def, speed, hp, max_hp);
        session->PostSend(statBuf.data(), statBuf.size());
    }

    std::vector<QuestSaveData> quests;
    if (query.FetchUserQuests(userId, quests))
    {
        for (const auto &quest : quests)
        {
            std::vector<char> questBuf = BuildQuestInfoAck(quest);
            session->PostSend(questBuf.data(), questBuf.size());
        }
    }

    std::vector<char> loginAckBuf =
        BuildLoginAck(0, session->GetServerUserId(), mapId, x, y, z, tutorialStep);
    session->PostSend(loginAckBuf.data(), loginAckBuf.size());

    std::vector<GameStruct::InventoryItem> items;
    if (query.FetchInventory(userId, items))
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

    if (!query.InsertUser(userId, password, duplicated))
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

bool PacketMethod::HandlePacket(Session *session, const NetPackets::PacketHeader &header, const char *payload,
                                size_t size)
{
    if (!session || !payload)
        return false;
    if (header.Id == static_cast<uint16_t>(NetPackets::PacketId::C2S_LOGIN_REQ) &&
        size == sizeof(NetPackets::PKT_LOGIN_REQ))
    {
        NetPackets::PKT_LOGIN_REQ packet{};
        std::memcpy(&packet, payload, sizeof packet);
        return HandleLoginRequest(session, packet);
    }
    if (header.Id == static_cast<uint16_t>(NetPackets::PacketId::C2S_SIGNUP_REQ) &&
        size == sizeof(NetPackets::PKT_SIGNUP_REQ))
    {
        NetPackets::PKT_SIGNUP_REQ packet{};
        std::memcpy(&packet, payload, sizeof packet);
        return HandleSignUpRequest(session, packet);
    }
    return false;
}
