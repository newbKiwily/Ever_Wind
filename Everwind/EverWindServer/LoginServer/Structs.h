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
    int maxEnemyCount;
    std::vector<int> enemyIdList;
    
};