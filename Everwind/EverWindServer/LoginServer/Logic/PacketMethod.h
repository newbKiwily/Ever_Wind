#pragma once

#include <string>
#include <vector>
#include <memory>
#include "../Network/Packet.h"
#include "../Network/Session.h"
#include "Structs.h"
class DBManager;
class Queries;
class IOCPServer;

class PacketMethod
{
public:
    explicit PacketMethod(DBManager& dbManager, IOCPServer* server);
    ~PacketMethod();
    bool HandlePacket(Session* session, const NetPackets::PacketHeader& header, const char* payload, size_t payloadSize);

    bool HandleLoginRequest(Session* session, const NetPackets::PKT_LOGIN_REQ& packet);
    bool HandleSignUpRequest(Session* session, const NetPackets::PKT_SIGNUP_REQ& packet);
    bool HandleMoveSyncRequest(Session* session, const NetPackets::PKT_MOVE_SYNC& packet);
    bool HandleInventoryRequest(Session* session, const NetPackets::PKT_INVENTORY_ITEM& packet);
    bool HandleStatRequest(Session* session, const NetPackets::PKT_USERSTAT& packet);
    bool HandleAttackRequest(Session* session, const NetPackets::PKT_C2S_ATTACK_REQ& packet);
    bool HandleEnemyMoveSyncRequest(Session* session, const NetPackets::PKT_ENEMY_MOVE_SYNC& packet);
    bool HandleEnemyAttackAnimRequest(Session* session, const NetPackets::PKT_ENEMY_ATTACK_ANIM& packet);
    bool HandleOneshotAnimReq(Session* session, const NetPackets::PKT_ONESHOT_ANIM_SYNC& packet);
    bool HandleInteractSyncReq(Session* session, const NetPackets::PKT_INTERACT_SYNC& packet);
    bool HandleDeadSyncReq(Session* session, const NetPackets::PKT_DEAD_SYNC& packet);
    bool HandleCombatStateSync(Session* session, const NetPackets::PKT_COMBAT_STATE_SYNC& packet);
    //change: Add a dedicated handler declaration for map-change requests.
    bool HandleMapChangeReq(Session* session, const NetPackets::PKT_MAP_CHANGE_REQ& packet);
    
    void SendPlayerLogOut(Session* session, int userid);

    Queries* getQuery() { return query_; }

private:

    std::vector<char> BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y, float z);
    std::vector<char> BuildSignUpAck(int resultCode);
    std::vector<char> BuildPlayerListAck(int playerDbId);
    std::vector<char> BuildMoveSyncAck(int playerDbId, float x, float y, float z, float speed, uint64_t timestamp);
    std::vector<char> BuildStatAck(int atk, int def, float speed, float hp, float max_hp);
    std::vector<char> BuildInventoryAck(const GameStruct::InventoryItem& item);
    std::vector<char> BuildLogoutAck(int playerDbId);
    std::vector<char> BuildPlayerListRuntime(int playerDbId);
    std::vector<char> BuildEnemySpawn(int instanceId, int enemyId, float x, float y, float z);
    std::vector<char> BuildEnemyDamaged(int instanceId, float currentHp, float damageAmount, int ownerDbId);
    std::vector<char> BuildEnemyMoveSync(int instanceId, float x, float y, float z);
    std::vector<char> BuildEnemyAttackAnim(int instanceId);
    std::vector<char> BuildOneshotAnimSync(int userDbId, int animCode);
    std::vector<char> BuildInteractSync(int userDbId, uint8_t isStart);
    std::vector<char> BuildDeadSync(int userDbId, uint8_t isStart);
    std::vector<char> BuildCombatStateSync(int userDbId, uint8_t isCombat);
    //change: Add a map-change ack builder declaration to complete the server packet interface.
    std::vector<char> BuildMapChangeAck(int mapId, float x, float y, float z);
private:
    DBManager& dbManager_;
    Queries* query_;
    IOCPServer* server_; 
};
