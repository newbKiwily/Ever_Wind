#include "MapData.h"
#include "../LoginServer/Network/Session.h"
#include <algorithm>
#include "Enemy.h"
#include <random>
#include <cmath>
#include <iostream>
MapData::MapData(float radius, float x, float y, float z, float playerSpawnX, float playerSpawnY, float playerSpawnZ, int maxEnemyCount, std::vector<int> enemyIdList)
    :spawnRadius(radius),spawnPosX(x),spawnPosY(y),spawnPosZ(z),
    playerSpawnPosX(playerSpawnX),playerSpawnPosY(playerSpawnY),playerSpawnPosZ(playerSpawnZ),
    maxEnemyCount(maxEnemyCount),enemyIdList(enemyIdList)
{
   
}
void MapData::AddSession(std::shared_ptr<Session> session) {
    if (!session) return;

    std::lock_guard<std::mutex> lock(mapMutex_);
    sessionsInMap_.push_back(session);
}

void MapData::RemoveSession(std::shared_ptr<Session> session) {
    if (!session) return;

    std::lock_guard<std::mutex> lock(mapMutex_);
    
    sessionsInMap_.erase(
        std::remove_if(sessionsInMap_.begin(), sessionsInMap_.end(),
            [&session](const std::weak_ptr<Session>& weak) {
        auto shared = weak.lock();
        return !shared || shared == session;
    }),
        sessionsInMap_.end()
    );
}

void MapData::BroadcastAll(const char* data, size_t size) {
    std::lock_guard<std::mutex> lock(mapMutex_);
    for (auto it = sessionsInMap_.begin(); it != sessionsInMap_.end(); ) {
        if (auto target = it->lock()) {
            target->PostSend(data, size);
            ++it;
        }
        else {
            it = sessionsInMap_.erase(it);
        }
    }
}

void MapData::BroadcastEx(std::shared_ptr<Session> sender, const char* data, size_t size) {
    std::lock_guard<std::mutex> lock(mapMutex_);
    for (auto it = sessionsInMap_.begin(); it != sessionsInMap_.end(); ) {
        if (auto target = it->lock()) {
            if (target != sender) {
                target->PostSend(data, size);
            }
            ++it;
        }
        else {
            it = sessionsInMap_.erase(it);
        }
    }
}

GameStruct::Vector3 MapData::GetPlayerSpawnPosition() const
{
    //change: Return the per-map player spawn position for map transitions.
    return GameStruct::Vector3{ playerSpawnPosX, playerSpawnPosY, playerSpawnPosZ };
}

std::weak_ptr<Enemy> MapData::findEnemy(int id)
{
    std::lock_guard<std::mutex> lock(mapMutex_);
    if (instancedEnemys.count(id)) 
        return instancedEnemys[id];
    return {};
}

void MapData::InstanceEnemy()
{
    std::lock_guard<std::mutex> lock(mapMutex_);
    
    if (enemyIdList.empty()||instancedEnemys.size() >= maxEnemyCount) return;

    static std::random_device rd;
    static std::mt19937 gen(rd());

    std::uniform_real_distribution<float> angleDist(0.0f, 3.1415926535f * 2.0f);
    std::uniform_real_distribution<float> radiusDist(0.0f, 1.0f);

    float r = spawnRadius * std::sqrt(radiusDist(gen));
    float theta = angleDist(gen);

    GameStruct::Vector3 spawnPos;
    spawnPos.x = spawnPosX + r * std::cos(theta);
    spawnPos.y = spawnPosY;
    spawnPos.z = spawnPosZ + r * std::sin(theta);

    instancedNum++;

    if (enemyIdListIndex < 0 || enemyIdListIndex >= enemyIdList.size())
    {
        enemyIdListIndex = 0;
    }

    int enemyId = enemyIdList[enemyIdListIndex++];

    auto newEnemy = std::make_shared<Enemy>(instancedNum, enemyId, spawnPos);
    instancedEnemys[instancedNum] = std::move(newEnemy);    
    std::cout << "Enemy ID: " << enemyId << "\n"
              << "Spawn position x y z: " << spawnPos.x << "," << spawnPos.y << "," << spawnPos.z << "\n";
}

void MapData::RemoveEnemy(int insId)
{
    std::lock_guard<std::mutex> lock(mapMutex_);

    instancedEnemys.erase(insId);

}