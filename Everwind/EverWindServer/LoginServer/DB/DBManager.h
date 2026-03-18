#pragma once

#include <mysql.h>
#include <string>
#include <mutex>

#pragma comment(lib, "libmariadb.lib")

class DBManager
{
public:
    DBManager();
    ~DBManager();

    bool Connect(const std::string& host, unsigned int port, const std::string& user, const std::string& password, const std::string& schema, const std::string& charset = "utf8mb4");
    void Disconnect();

    MYSQL* GetConnection() const { return connection_; }
    std::mutex& GetMutex() { return mutex_; }

private:
    MYSQL* connection_;
    std::mutex mutex_;
};

