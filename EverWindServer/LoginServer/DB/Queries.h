#pragma once
class Queries;
#include "DBManager.h"
#include "../Gameplay/CoreData/GameDataHub.h"
#include <string>
#include <vector>
#include <unordered_map>
#include "../Network/Packet.h"
#include "../Structs.h"
class Queries
{
  private:
    DBManager &db_;
    // 콜백 연결 전에 초기화하고 요청 처리 중에는 읽기만 한다.
    std::unordered_map<int, std::string> keys_;

    std::unordered_map<std::string, int> ids_;

    std::unordered_map<int, int> slots_;
  public:
    explicit Queries(DBManager &db);

    void InitializeGameplayItems(const std::unordered_map<int, EverWind::Gameplay::ItemDefinition> &items);

    EverWind::Gameplay::AccountState LoadGameplayState(const std::string &user);

    bool SaveGameplayState(const std::string &user, const EverWind::Gameplay::AccountState &state);

    EverWind::Gameplay::GameDataHub LoadGameDefinitions();

    bool EnsureMapIds(const std::vector<int> &ids);

    bool FetchUser(const std::string &userId, std::string &outUserId, int &outMapId, float &outPosX, float &outPosY, float &outPosZ, int &outTutorialStep, bool &outFound, bool *outHasPosition = nullptr);

    bool InsertUser(const std::string &userId, const std::string &password, bool &duplicated);

    bool VerifyPassword(const std::string &userId, const std::string &password, bool &matches);

    bool UpdateUserPosition(const std::string &userId, int mapId, float x, float y, float z);

    bool FetchInventory(const std::string &userId, std::vector<GameStruct::InventoryItem> &outItems);

    bool FetchUserQuests(const std::string &userId, std::vector<QuestSaveData> &outQuests);

    bool FetchUserStat(const std::string &userId, int &attack, int &defence, float &speed, float &hp, float &max_hp);

    bool UpdateInventory(const std::string &userId, const NetPackets::PKT_INVENTORY_ITEM &item);

    bool UpdateUserStat(const std::string &userId, const NetPackets::PKT_USERSTAT &stat);

    bool ResetUserQuests(const std::string &userId);

    bool UpdateUserQuest(const std::string &userId, const NetPackets::PKT_QUEST_DATA &quest);
};
