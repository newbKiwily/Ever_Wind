#include "SessionManager.h"
#include "../LoginServer/Network/Session.h"
int SessionManager::AddSession(std::shared_ptr<Session> session)
{
    int count = assignedId.fetch_add(1);

    std::lock_guard<std::mutex> lock(mutex_);
    sessions_[count] = session;
    return count;
}

std::shared_ptr<Session> SessionManager::GetSession(int serverUserId)
{
    std::lock_guard<std::mutex> lock(mutex_);
    auto it = sessions_.find(serverUserId);
    if (it != sessions_.end()) {
        return it->second;
    }
    return nullptr;
}

void SessionManager::RemoveSession(int serverUserId) {
    std::lock_guard<std::mutex> lock(mutex_);
    sessions_.erase(serverUserId);
}

void SessionManager::BroadcastAll(const char* data, size_t size) {
    std::lock_guard<std::mutex> lock(mutex_);
    for (auto& pair : sessions_) {
        if (pair.second) {
            pair.second->PostSend(data, size);
        }
    }
}

void SessionManager::BroadcastEx(std::shared_ptr<Session> sender, const char* data, size_t size) {
    std::lock_guard<std::mutex> lock(mutex_);
    for (auto& pair : sessions_) {
        if (pair.second && pair.second != sender) {
            pair.second->PostSend(data, size);
        }
    }
}

bool SessionManager::isSessionLogin(Session* session)
{
    if (session->GetUserId() == "")
        return false;
    return true;
}
