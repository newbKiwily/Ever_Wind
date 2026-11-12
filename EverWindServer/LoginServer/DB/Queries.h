#pragma once
class Queries;
#include "DBManager.h"

#include <string>
#include <vector>

#include "../Network/Packet.h"
#include "Structs.h"

class Queries
{
public:
    explicit Queries(DBManager& db);

    bool FetchUser(const std::string& userId, std::string& outUserId, int& outMapId,
        float& outPosX, float& outPosY, float& outPosZ, int& outTutorialStep, bool& outFound);

    bool InsertUser(const std::string& userId, const std::string& password, bool& duplicated);

    bool VerifyPassword(const std::string& userId, const std::string& password, bool& matches);

    bool UpdateUserPosition(const std::string& userId, int mapId, float x, float y, float z);

    bool FetchInventory(const std::string& userId, std::vector<GameStruct::InventoryItem>& outItems);
    bool FetchUserQuests(const std::string& userId, std::vector<QuestSaveData>& outQuests);

    bool FetchUserStat(const std::string& userId, int& attack, int& defence, float& speed, float& hp, float& max_hp);
    bool FetchMapdata(const std::string& userId, int& outMapId,float& outSpawnEnemyX, float& outSpawnEnemyY, float& outSpawnEnemyZ,float& outSpawnRadius, int& outMaxEnemyCount,std::vector<int>& outMonsterIds);
    bool FetchAllMapData(std::vector<MapInitialInfo>& outMaps);

    bool UpdateInventory(const std::string& userId, const NetPackets::PKT_INVENTORY_ITEM& item);
    bool UpdateUserStat(const std::string& userId, const NetPackets::PKT_USERSTAT& stat);
    bool ResetUserQuests(const std::string& userId);
    bool UpdateUserQuest(const std::string& userId, const NetPackets::PKT_QUEST_DATA& quest);

private:
    DBManager& db_;
};
