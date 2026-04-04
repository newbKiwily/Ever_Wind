#pragma once
#include <unordered_map>
#include <memory.h>
#include "../LoginServer/Network/Session.h"
#include "Structs.h"
class MapData;
class SessionManager;
class Enemy;
class PacketMethod;
class MapDataManager
{
public:
	MapDataManager(SessionManager* sessionManager,PacketMethod* packetMethod);
	~MapDataManager();

	MapData* findMapData(int id);
	bool changeMap(std::shared_ptr<Session> session, int targetMapId, GameStruct::Vector3& outSpawnPosition);
	void broadcastAll(int id, const char* data, size_t size); // 서버 주도 맵 방송
	void broadcastEx(int id, std::shared_ptr<Session> sender, const char* data, size_t size);
	void RegisterMap(int id, MapData* mapData);
	void RefillEnemyAllMap();
private:
	SessionManager* sessionManager_;
	PacketMethod* packetMethod_;
	std::unordered_map<int, MapData*> mapTable;
};

