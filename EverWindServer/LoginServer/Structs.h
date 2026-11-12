#pragma once
#include <vector>

namespace GameStruct
{
struct InventoryItem
{
    char ItemId[32];

    int amount;
    int slotIndex;
    int isEquipped;
};
} // namespace GameStruct

struct QuestSaveData
{
    int questId = 0;
    bool isCompleted = false;
    bool rewardClaimed = false;
    std::vector<int> currentCounts;
};

namespace GameStruct
{
struct Vector3
{
    float x, y, z;
};
} // namespace GameStruct
