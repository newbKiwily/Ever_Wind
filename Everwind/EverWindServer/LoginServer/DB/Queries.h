#pragma once

#include <string>
#include <vector>
#include "../Network/Session.h"
#include "../Network/Packet.h"
#include "Structs.h"
class DBManager;
class Packet;
class Queries
{
public:
    explicit Queries(DBManager& db);

    bool FetchUser(const std::string& userId, std::string& outUserId, std::string& outPasswordHash, std::string& outSalt, int& outMapId,
        float& outPosX, float& outPosY, float& outPosZ, bool& outFound);

    bool InsertUser(const std::string& userId, const std::string& passwordHash, const std::string& salt, bool& duplicated);

    bool UpdateUserPosition(const std::string& userId, int mapId, float x, float y, float z);

    bool FetchInventory(const std::string& userId, std::vector<GameStruct::InventoryItem>& outItems);

    bool FetchUserStat(const std::string& userId, int& attack, int& defence, float& speed, float& hp, float& max_hp);
    bool FetchMapdata(const std::string& userId, int& outMapId,float& outSpawnEnemyX, float& outSpawnEnemyY, float& outSpawnEnemyZ,float& outSpawnRadius, int& outMaxEnemyCount,std::vector<int>& outMonsterIds);
    bool FetchAllMapData(std::vector<MapInitialInfo>& outMaps);

    bool UpdateInventory(const std::string& userId, const NetPackets::PKT_INVENTORY_ITEM& item);
    bool UpdateUserStat(const std::string& userId, const NetPackets::PKT_USERSTAT& stat);
   
private:
    DBManager& db_;
};