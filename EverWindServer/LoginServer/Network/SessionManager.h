#pragma once
class SessionManager;
#include "Session.h"
#include <mutex>
#include <unordered_map>
#include <memory.h>
#include <atomic>
#include <string>
#include <vector>

class SessionManager
{
  public:
    SessionManager();
    void CloseAll();

    int AddSession(std::shared_ptr<Session> session);

    std::shared_ptr<Session> GetSession(int serverUserId);

    void RemoveSession(int serverUserId);

    void BroadcastAll(const char *data, size_t size);
    void BroadcastEx(std::shared_ptr<Session> sender, const char *data, size_t size); // 발신자 제외

    bool IsExistSession(Session *session);
    bool isSessionLogin(Session *session);
    bool TryAuthenticate(Session *session, const std::string &user);

  private:
    std::atomic<int> assignedId;
    std::mutex mutex_;
    std::unordered_map<int, std::shared_ptr<Session>> sessions_;
};
