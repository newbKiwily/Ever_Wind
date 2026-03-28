#include "MapDataManager.h"
#include "MapData.h"
#include "SessionManager.h"
MapDataManager::MapDataManager(SessionManager* sessionManager)
{	
	sessionManager_ = sessionManager;
	
}

MapDataManager::~MapDataManager()
{
}

MapData* MapDataManager::findMapData(int id)
{
	return mapTable[id];
}

bool MapDataManager::changeMap(std::shared_ptr<Session> session, int targetMapId, GameStruct::Vector3& outSpawnPosition)
{
    if (!session) return false;

    int oldMapId = session->GetMapId();
    MapData* oldMap = findMapData(oldMapId);
    MapData* targetMap = findMapData(targetMapId);

    if (!targetMap) return false;

    //change: Remove the session from the previous map before switching context.
    if (oldMap) {
        oldMap->RemoveSession(session);
    }

    //change: Move the session to the target map's configured player spawn point.
    outSpawnPosition = targetMap->GetPlayerSpawnPosition();
    session->SetMapId(targetMapId);
    session->SetPosition(outSpawnPosition.x, outSpawnPosition.y, outSpawnPosition.z);
    targetMap->AddSession(session);

    return true;
}


void MapDataManager::broadcastAll(int id, const char* data, size_t size) {
	MapData* mapData = findMapData(id);
	if (mapData) {
		mapData->BroadcastAll(data, size);
	}
}

void MapDataManager::broadcastEx(int id, std::shared_ptr<Session> sender, const char* data, size_t size) {
	MapData* mapData = findMapData(id);
	if (mapData) {
		mapData->BroadcastEx(sender, data, size);
	}
}

void MapDataManager::RegisterMap(int id, MapData* mapData)
{
    if (mapTable.find(id) != mapTable.end()) {
        delete mapTable[id]; // 이미 존재한다면 교체 (메모리 관리 주의)
    }
    mapTable[id] = mapData;
}