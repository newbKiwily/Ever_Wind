#pragma once
#include <unordered_map>
#include <memory.h>
#include "../LoginServer/Network/Session.h"
#include "Structs.h"
class MapData;
class SessionManager;
class MapDataManager
{
public:
	MapDataManager(SessionManager* sessionManager);
	~MapDataManager();

	MapData* findMapData(int id);
	//change: Return success and the target spawn position so packet handling can finish the transition.
	bool changeMap(std::shared_ptr<Session> session, int targetMapId, GameStruct::Vector3& outSpawnPosition);
	void broadcastAll(int id, const char* data, size_t size); // 서버 주도 맵 방송
	void broadcastEx(int id, std::shared_ptr<Session> sender, const char* data, size_t size);
	void RegisterMap(int id, MapData* mapData);
private:
	SessionManager* sessionManager_;
	std::unordered_map<int, MapData*> mapTable;
};

