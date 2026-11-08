#pragma once
#include <vector>
#include <mutex>
#include <memory>
#include <unordered_map>
#include "Structs.h"
class Session;
class Enemy;
class MapData {
public:
    
    
    MapData(float radius,float x,float y,float z,float playerSpawnX,float playerSpawnY,float playerSpawnZ,int maxEnemyCount,std::vector<int> enemyIdList);
    void AddSession(std::shared_ptr<Session> session);

    void RemoveSession(std::shared_ptr<Session> session);

    void BroadcastAll(const char* data, size_t size); // 맵 내 전원
    void BroadcastEx(std::shared_ptr<Session> sender, const char* data, size_t size); // 맵 내 발신자 제외

    const std::vector<std::weak_ptr<Session>>& getSessionInMap() { return sessionsInMap_; }
    const std::unordered_map<int, std::shared_ptr<Enemy>>& getInstancedEnemies() const
    {
        return instancedEnemys;
    }
    GameStruct::Vector3 GetPlayerSpawnPosition() const;
    std::weak_ptr<Enemy> findEnemy(int id);
    std::shared_ptr<Enemy> InstanceEnemy();
    std::vector<std::shared_ptr<Enemy>> RefillEnemy();
    void RemoveEnemy(int insId);
private:
    std::shared_ptr<Enemy> InstanceEnemyUnlocked();

    std::mutex mapMutex_;
    std::vector<std::weak_ptr<Session>> sessionsInMap_;
    std::unordered_map< int, std::shared_ptr<Enemy>> instancedEnemys;
    std::vector<int> enemyIdList;
    int instancedNum = 0;
    int enemyIdListIndex = 0;
    float spawnRadius;
    float spawnPosX, spawnPosY, spawnPosZ;
    float playerSpawnPosX, playerSpawnPosY, playerSpawnPosZ;
    int maxEnemyCount;
    
};
