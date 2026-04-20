#pragma once
#include <vector>
namespace GameStruct 
{   

    struct Vector3
    {
        float x, y, z;
    };

    struct InventoryItem
    {
        char ItemId[32];

        int amount;
        int slotIndex;
        int isEquipped;
    };
}

struct MapInitialInfo {
    int mapId;
    float enemySpawnX, enemySpawnY, enemySpawnZ, radius;
    float playerSpawnX, playerSpawnY, playerSpawnZ;
    int maxEnemyCount;
    std::vector<int> enemyIdList;
    
};

struct QuestSaveData
{
    int questId = 0;
    bool isCompleted = false;
    bool rewardClaimed = false;
    std::vector<int> currentCounts;
};
