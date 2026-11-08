#pragma once
#include <mutex>
#include <unordered_map>
#include <memory.h>
#include <atomic>
#include <string>
#include <vector>
#include "MapDataManager.h"

class Session;
class PacketMethod;
class SessionManager {
public:
    SessionManager(PacketMethod* packetMethod) : assignedId(1)
    {
        mapDataManager_ = std::make_unique<MapDataManager>(this,packetMethod);
    }

    int AddSession(std::shared_ptr<Session> session);

    std::shared_ptr<Session> GetSession(int serverUserId);
    
    void RemoveSession(int serverUserId); 

    void BroadcastAll(const char* data, size_t size); 
    void BroadcastEx(std::shared_ptr<Session> sender, const char* data, size_t size); // 발신자 제외
    
    bool IsExistSession(Session* session) {
        if (session == nullptr) return false;

        std::lock_guard<std::mutex> lock(mutex_);

        for (auto& pair : sessions_) {
            if (pair.second.get() == session) {
                return true;
            }
        }
        return false;
    }
    bool isSessionLogin(Session* session);
   

    MapDataManager* GetMapDataManager() { return mapDataManager_.get(); }

private:
    std::atomic<int> assignedId;
    std::mutex mutex_;
    std::unordered_map<int, std::shared_ptr<Session>> sessions_;
    std::unique_ptr<MapDataManager> mapDataManager_;
};