#include "PacketMethod.h"
#include "../DB/DBManager.h"
#include "../DB/Queries.h"
#include "../Network/IOCPServer.h"
#include "../Util/SHA256.h"
#include "SessionManager.h"
#include "MapDataManager.h"
#include "MapData.h"
#include <cstring>
#include <iostream>
#include "Enemy.h"
namespace

{
    size_t SafeStringLength(const char* str, size_t maxLen)
    {
        if (!str) return 0;
        size_t len = 0;

        while (len < maxLen && str[len] != '\0') ++len;

        return len;
    }

}
PacketMethod::PacketMethod(DBManager& dbManager, IOCPServer* server)
    : dbManager_(dbManager), server_(server)
{
    query_ = new Queries(dbManager_);
}

PacketMethod::~PacketMethod() { delete query_; }


std::vector<char> PacketMethod::BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y, float z) {
    NetPackets::PKT_LOGIN_ACK ack{ resultCode, userDbId, mapId, x, y, z };

    NetPackets::PacketHeader header;
    header.Length = static_cast<uint16_t>(sizeof(header) + sizeof(ack)); 
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_LOGIN_ACK);   

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildPlayerListAck(int playerDbId) {
    NetPackets::PKT_PLAYER_LIST_ACK ack{ playerDbId };

    NetPackets::PacketHeader header;
    header.Length = static_cast<uint16_t>(sizeof(header) + sizeof(ack));
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_PLAYERLIST_ACK);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildMoveSyncAck(int playerDbId, float x, float y, float z, float speed, uint64_t timestamp) {
    NetPackets::PKT_MOVE_SYNC ack{ playerDbId, x, y, z, speed, (uint32_t)timestamp }; 

    NetPackets::PacketHeader header;
    header.Length = static_cast<uint16_t>(sizeof(header) + sizeof(ack));
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::SC2_MOVESYNC_ACK);

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

    std::cout << "[회원가입 응답 패킷 생성] Result: " << resultCode << "\n";
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

    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_PLAYERSTAT);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

std::vector<char> PacketMethod::BuildInventoryAck(const GameStruct::InventoryItem& item)
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

std::vector<char> PacketMethod::BuildLogoutAck(int playerDbId)
{
    NetPackets::PKT_LOGOUT_ACK ack{};
    ack.UserDBID = playerDbId;

    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_LOGOUT_ACK);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}
std::vector<char> PacketMethod::BuildPlayerListRuntime(int playerDbId)
{
    NetPackets::PKT_PLAYER_LIST_RUNTIME_ACK ack{ playerDbId };

    NetPackets::PacketHeader header;
    header.Length = static_cast<uint16_t>(sizeof(header) + sizeof(ack));
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_PLAYERLIST_RUNTIME);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}

std::vector<char> PacketMethod::BuildEnemySpawn(int instanceId, int enemyId, float x, float y, float z)
{
    NetPackets::PKT_ENEMY_SPAWN ack{};
    ack.instanceId = instanceId;
    ack.enemyId = enemyId;
    ack.posX = x;
    ack.posY = y;
    ack.posZ = z; 

    NetPackets::PacketHeader header{};

    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_ENEMY_SPAWN);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));

    return buffer;
}
std::vector<char> PacketMethod::BuildEnemyDamaged(int instanceId, float currentHp, float damageAmount, int ownerDbId)
{
    NetPackets::PKT_S2C_ENEMY_DAMAGED ack{ instanceId, currentHp, damageAmount, ownerDbId };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_ENEMY_DAMAGED);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildEnemyMoveSync(int instanceId, float x, float y, float z)
{
    NetPackets::PKT_ENEMY_MOVE_SYNC ack{ instanceId, x, y, z };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_ENEMY_MOVE_SYNC);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildEnemyAttackAnim(int instanceId)
{
    NetPackets::PKT_ENEMY_ATTACK_ANIM ack{ instanceId };

    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::S2C_ENEMY_ATTACK_ANIM);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}
std::vector<char> PacketMethod::BuildOneshotAnimSync(int userDbId, int animCode)
{
    NetPackets::PKT_ONESHOT_ANIM_SYNC ack{ userDbId, animCode };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_ONESHOT_ANIM_SYNC);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildInteractSync(int userDbId, uint8_t isStart)
{
    NetPackets::PKT_INTERACT_SYNC ack{ userDbId, isStart };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_INTERACT_SYNC);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildDeadSync(int userDbId, uint8_t isStart)
{
    NetPackets::PKT_DEAD_SYNC ack{ userDbId, isStart };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_DEAD_SYNC);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}

std::vector<char> PacketMethod::BuildCombatStateSync(int userDbId, uint8_t isCombat)
{
    NetPackets::PKT_COMBAT_STATE_SYNC ack{ userDbId, isCombat };
    NetPackets::PacketHeader header{};
    header.Id = static_cast<uint16_t>(NetPackets::PacketId::_COMBAT_STATE_SYNC);
    header.Length = sizeof(header) + sizeof(ack);

    std::vector<char> buffer(header.Length);
    std::memcpy(buffer.data(), &header, sizeof(header));
    std::memcpy(buffer.data() + sizeof(header), &ack, sizeof(ack));
    return buffer;
}
bool PacketMethod::HandleEnemyAttackAnimRequest(Session* session, const NetPackets::PKT_ENEMY_ATTACK_ANIM& packet)
{
    if (!session) return false;

    auto mapMgr = server_->GetSessionManager()->GetMapDataManager();
    auto mapData = mapMgr->findMapData(session->GetMapId());
    if (!mapData) return false;

    auto enemyWeak = mapData->findEnemy(packet.instanceId);
    if (auto enemy = enemyWeak.lock())
    {
        if (enemy->getOwnerDbId() == session->GetServerUserId())
        {
            std::vector<char> buffer = BuildEnemyAttackAnim(packet.instanceId);
            mapData->BroadcastEx(session->shared_from_this(), buffer.data(), buffer.size());
        }
    }
    return true;
}


bool PacketMethod::HandleLoginRequest(Session* session, const NetPackets::PKT_LOGIN_REQ& packet)
{

    std::string userId(packet.UserID, SafeStringLength(packet.UserID, sizeof(packet.UserID)));
    std::string password(packet.Password, SafeStringLength(packet.Password, sizeof(packet.Password)));

    std::cout << "[로그인 요청 수신] UserID: " << userId << "\n";

    if (userId.empty() || password.empty())
    {
        std::vector<char> failAck = BuildLoginAck(1, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return false;
    }

    std::string dbUserId;
    int mapId = 0;
    float x = 0, y = 0, z = 0;
    std::string dbHash, dbSalt;
    bool found = false;

    if (!query_->FetchUser(userId, dbUserId, dbHash, dbSalt, mapId, x, y, z, found) || !found)
    {
        std::vector<char> failAck = BuildLoginAck(1, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return true;
    }

    SHA256 sha;
    if (sha.CalculateHex(password + dbSalt) != dbHash)
    {
        std::vector<char> failAck = BuildLoginAck(2, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return true;
    }

    if (server_->GetSessionManager()->isSessionLogin(session))
    {
        std::vector<char> failAck = BuildLoginAck(3, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return true;
    }

    session->SetUserId(dbUserId);
    session->SetMapId(mapId);
    session->SetPosition(x, y, z);

    auto mapMgr = server_->GetSessionManager()->GetMapDataManager();
    auto mapData = mapMgr->findMapData(mapId);

    if (!mapData)
    {
        std::cerr << "[MapData Error] Map " << mapId << " not found in memory." << std::endl;
        std::vector<char> failAck = BuildLoginAck(4, 0, 0, 0, 0, 0);
        session->PostSend(failAck.data(), failAck.size());
        return false;
    }

    mapData->AddSession(session->shared_from_this());
    std::vector<char> myInfoBuf = BuildPlayerListRuntime(session->GetServerUserId());
    mapMgr->broadcastEx(mapId, session->shared_from_this(), myInfoBuf.data(), myInfoBuf.size());

    for (auto& weak : mapData->getSessionInMap())
    {
        if (auto other = weak.lock())
        {
            if (other.get() == session) continue;
            std::vector<char> otherInfoBuf = BuildPlayerListAck(other->GetServerUserId());
            session->PostSend(otherInfoBuf.data(), otherInfoBuf.size());
        }
    }

    const auto& enemies = mapData->getInstancedEnemies();
    for (auto& pair : enemies)
    {
        auto& enemy = pair.second;
        auto position = enemy->getCurrentPosition();
        auto instanceId = enemy->getInstancNum();
        auto enemyId = enemy->getEnemyId();

        std::vector<char> enemySpawnBuf = BuildEnemySpawn(
            instanceId,
            enemyId,
            position.x,
            position.y,
            position.z
        );
        session->PostSend(enemySpawnBuf.data(), enemySpawnBuf.size());
    }

    int atk, def;
    float speed, hp, max_hp;
    if (query_->FetchUserStat(userId, atk, def, speed, hp, max_hp))
    {
        std::vector<char> statBuf = BuildStatAck(atk, def, speed, hp, max_hp);
        session->PostSend(statBuf.data(), statBuf.size());
    }

    std::vector<char> loginAckBuf = BuildLoginAck(0, session->GetServerUserId(), mapId, x, y, z);
    session->PostSend(loginAckBuf.data(), loginAckBuf.size());

    std::vector<GameStruct::InventoryItem> items;
    if (query_->FetchInventory(userId, items))
    {
        for (const auto& item : items)
        {
            std::vector<char> invBuf = BuildInventoryAck(item);
            session->PostSend(invBuf.data(), invBuf.size());
        }
    }

    return true;
}

bool PacketMethod::HandleMoveSyncRequest(Session* session, const NetPackets::PKT_MOVE_SYNC& packet) {
    if (!session) return false;

    session->SetPosition(packet.PosX, packet.PosY, packet.PosZ);

    std::vector<char> moveBuf = BuildMoveSyncAck(packet.UserDBID, packet.PosX, packet.PosY, packet.PosZ, packet.Speed, packet.Timestamp);

    server_->GetSessionManager()->GetMapDataManager()->broadcastEx(
        session->GetMapId(),
        session->shared_from_this(),
        moveBuf.data(),
        moveBuf.size()
    );

    return true;
}

bool PacketMethod::HandleInventoryRequest(Session* session, const NetPackets::PKT_INVENTORY_ITEM& packet)
{
    if (!session || session->GetUserId().empty()) return false;

    if (!query_->UpdateInventory(session->GetUserId(), packet))
    {
        std::cerr << "[Inventory Update Fail] User: " << session->GetUserId() << std::endl;
        return false;
    }

    return true;
}

bool PacketMethod::HandleStatRequest(Session* session, const NetPackets::PKT_USERSTAT& packet)
{
    if (!session || session->GetUserId().empty()) return false;

    if (!query_->UpdateUserStat(session->GetUserId(), packet))
    {
        std::cerr << "[Stat Update Fail] User: " << session->GetUserId() << std::endl;
        return false;
    }

    return true;
}

void PacketMethod::SendPlayerLogOut(Session* session, int userid) {
    
    std::vector<char> buffer = BuildLogoutAck(userid);

    server_->GetSessionManager()->GetMapDataManager()->broadcastAll(
        session->GetMapId(),
        buffer.data(),
        buffer.size()
    );
}

bool PacketMethod::HandlePacket(Session* session, const NetPackets::PacketHeader& header, const char* payload, size_t payloadSize)
{
    if (!session) return false;

    switch (static_cast<NetPackets::PacketId>(header.Id))
    {
    case NetPackets::PacketId::C2S_LOGIN_REQ :
    {
        if (payloadSize != sizeof(NetPackets::PKT_LOGIN_REQ)) return false;
        NetPackets::PKT_LOGIN_REQ pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleLoginRequest(session, pkt);
    }
    case NetPackets::PacketId::C2S_SIGNUP_REQ:
    {
        if (payloadSize != sizeof(NetPackets::PKT_SIGNUP_REQ)) return false;
        NetPackets::PKT_SIGNUP_REQ pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleSignUpRequest(session, pkt);
    }
    case NetPackets::PacketId::C2S_MOVESYNC_REQ:
    {
        if (payloadSize != sizeof(NetPackets::PKT_MOVE_SYNC)) return false;
        NetPackets::PKT_MOVE_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleMoveSyncRequest(session, pkt);
    }
    case NetPackets::PacketId::_INVENTORYITEM:
    {
        if (payloadSize != sizeof(NetPackets::PKT_INVENTORY_ITEM)) return false;
        NetPackets::PKT_INVENTORY_ITEM pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleInventoryRequest(session, pkt);
    }
    case NetPackets::PacketId::_PLAYERSTAT:
    {
        if (payloadSize != sizeof(NetPackets::PKT_USERSTAT)) return false;
        NetPackets::PKT_USERSTAT pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleStatRequest(session, pkt);
    }
    case NetPackets::PacketId::C2S_ATTACK_REQ:
    {
        if (payloadSize != sizeof(NetPackets::PKT_C2S_ATTACK_REQ)) return false;
        NetPackets::PKT_C2S_ATTACK_REQ pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleAttackRequest(session, pkt);
    }
    case NetPackets::PacketId::C2S_ENEMY_MOVE_SYNC:
    {
        if (payloadSize != sizeof(NetPackets::PKT_ENEMY_MOVE_SYNC)) return false;
        NetPackets::PKT_ENEMY_MOVE_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleEnemyMoveSyncRequest(session, pkt);
    }
    case NetPackets::PacketId::C2S_ENEMY_ATTACK_ANIM:
    {
        if (payloadSize != sizeof(NetPackets::PKT_ENEMY_ATTACK_ANIM)) return false;
        NetPackets::PKT_ENEMY_ATTACK_ANIM pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleEnemyAttackAnimRequest(session, pkt);
    }
    case NetPackets::PacketId::_ONESHOT_ANIM_SYNC:
    {
        if (payloadSize != sizeof(NetPackets::PKT_ONESHOT_ANIM_SYNC)) return false;
        NetPackets::PKT_ONESHOT_ANIM_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleOneshotAnimReq(session, pkt);
    }
    case NetPackets::PacketId::_INTERACT_SYNC:
    {
        if (payloadSize != sizeof(NetPackets::PKT_INTERACT_SYNC)) return false;
        NetPackets::PKT_INTERACT_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleInteractSyncReq(session, pkt);
    }
    case NetPackets::PacketId::_DEAD_SYNC:
    {
        if (payloadSize != sizeof(NetPackets::PKT_DEAD_SYNC)) return false;
        NetPackets::PKT_DEAD_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleDeadSyncReq(session, pkt);
    }
    case NetPackets::PacketId::_COMBAT_STATE_SYNC:
    {
        if (payloadSize != sizeof(NetPackets::PKT_COMBAT_STATE_SYNC)) return false;
        NetPackets::PKT_COMBAT_STATE_SYNC pkt{};
        std::memcpy(&pkt, payload, sizeof(pkt));
        return HandleCombatStateSync(session, pkt);
    }
    default:
        // 처리할 수 없는 패킷 ID
        return false;
    }
}

bool PacketMethod::HandleSignUpRequest(Session* session, const NetPackets::PKT_SIGNUP_REQ& packet)
{

    std::string userId(packet.UserID, SafeStringLength(packet.UserID, sizeof(packet.UserID)));
    std::string password(packet.Password, SafeStringLength(packet.Password, sizeof(packet.Password)));

    std::cout << "[회원가입 요청 수신] UserID: " << userId << "\n";

    if (userId.empty() || password.empty())
    {
        std::vector<char> failAck = BuildSignUpAck(2);
        session->PostSend(failAck.data(), failAck.size());
        return false;
    }

    std::string salt = "RANDOM_SALT";
    SHA256 sha;
    std::string hash = sha.CalculateHex(password + salt);

    bool duplicated = false;

    if (!query_->InsertUser(userId, hash, salt, duplicated))
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

bool PacketMethod::HandleAttackRequest(Session* session, const NetPackets::PKT_C2S_ATTACK_REQ& packet)
{
    if (!session) return false;

    auto mapMgr = server_->GetSessionManager()->GetMapDataManager();
    auto mapData = mapMgr->findMapData(session->GetMapId());
    if (!mapData) return false;

    auto enemyWeak = mapData->findEnemy(packet.instanceId);
    if (auto enemy = enemyWeak.lock())
    {
        enemy->takeDamage(packet.damage);

        int attackerDbId = session->GetServerUserId();
        enemy->setOwnerDbId(attackerDbId);

        std::vector<char> buffer = BuildEnemyDamaged(packet.instanceId, enemy->getHp(), packet.damage, attackerDbId);
        mapData->BroadcastAll(buffer.data(), buffer.size());
    }
    return true;
}

bool PacketMethod::HandleEnemyMoveSyncRequest(Session* session, const NetPackets::PKT_ENEMY_MOVE_SYNC& packet)
{
    if (!session) return false;

    auto mapMgr = server_->GetSessionManager()->GetMapDataManager();
    auto mapData = mapMgr->findMapData(session->GetMapId());
    if (!mapData) return false;

    auto enemyWeak = mapData->findEnemy(packet.instanceId);
    if (auto enemy = enemyWeak.lock())
    {
        if (enemy->getOwnerDbId() == session->GetServerUserId())
        {
            enemy->setCurrentPosition({ packet.posX, packet.posY, packet.posZ });

            std::vector<char> buffer = BuildEnemyMoveSync(packet.instanceId, packet.posX, packet.posY, packet.posZ);
            mapData->BroadcastEx(session->shared_from_this(), buffer.data(), buffer.size());
        }
    }
    return true;
}

bool PacketMethod::HandleOneshotAnimReq(Session* session, const NetPackets::PKT_ONESHOT_ANIM_SYNC& packet)
{
    if (!session) return false;

    // 클라이언트가 보낸 내용을 그대로 나를 제외한(BroadcastEx) 맵 유저들에게 브로드캐스팅
    std::vector<char> buffer = BuildOneshotAnimSync(packet.UserDBID, packet.AnimCode);
    server_->GetSessionManager()->GetMapDataManager()->broadcastEx(
        session->GetMapId(),
        session->shared_from_this(),
        buffer.data(),
        buffer.size()
    );

    return true;
}

bool PacketMethod::HandleInteractSyncReq(Session* session, const NetPackets::PKT_INTERACT_SYNC& packet)
{
    if (!session) return false;

    std::vector<char> buffer = BuildInteractSync(packet.UserDBID, packet.isStart);
    server_->GetSessionManager()->GetMapDataManager()->broadcastEx(
        session->GetMapId(),
        session->shared_from_this(),
        buffer.data(),
        buffer.size()
    );

    return true;
}

bool PacketMethod::HandleDeadSyncReq(Session* session, const NetPackets::PKT_DEAD_SYNC& packet)
{
    if (!session) return false;

    std::vector<char> buffer = BuildDeadSync(packet.UserDBID, packet.isStart);
    server_->GetSessionManager()->GetMapDataManager()->broadcastEx(
        session->GetMapId(),
        session->shared_from_this(),
        buffer.data(),
        buffer.size()
    );

    return true;
}

bool PacketMethod::HandleCombatStateSync(Session* session, const NetPackets::PKT_COMBAT_STATE_SYNC& packet)
{
    if (!session) return false;

    std::vector<char> buffer = BuildCombatStateSync(packet.UserDBID, packet.isCombat);
    server_->GetSessionManager()->GetMapDataManager()->broadcastEx(
        session->GetMapId(),
        session->shared_from_this(),
        buffer.data(),
        buffer.size()
    );

    return true;
}