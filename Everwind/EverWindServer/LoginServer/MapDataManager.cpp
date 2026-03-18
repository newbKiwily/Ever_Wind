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

void MapDataManager::changeMap(std::shared_ptr<Session> session, int targetMapId)
{
    if (!session) return;

    int oldMapId = session->GetMapId();
    MapData* oldMap = findMapData(oldMapId);
    MapData* targetMap = findMapData(targetMapId);

    if (!targetMap) return; // 이동할 맵이 없으면 중단

    // 1. 기존 맵에서 제거 및 퇴장 알림
    if (oldMap) {
        oldMap->RemoveSession(session);

        // 주변 사람들에게 내가 사라짐을 알림 (BroadcastEx 사용)
        // NetPackets::PKT_LOGOUT_ACK 또는 별도의 LEAVE_MAP 패킷 활용
        // 예: session->GetServer()->getPacketMethod()->SendPlayerLeave(oldMapId, session);
    }

    // 2. 세션 정보 갱신 및 새 맵 추가
    session->SetMapId(targetMapId);
    targetMap->AddSession(session);

    // 3. 새 맵 유저들에게 나의 입장 알림 및 나에게 주변 유저 목록 전송
    // 이 부분은 PacketMethod의 로직을 활용하거나 별도 함수로 분리
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