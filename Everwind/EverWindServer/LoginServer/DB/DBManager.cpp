#include "DBManager.h"

#include <stdexcept>

DBManager::DBManager()
    : connection_(mysql_init(nullptr))
{
}

DBManager::~DBManager()
{
    Disconnect();
}

bool DBManager::Connect(const std::string& host, unsigned int port, const std::string& user, const std::string& password, const std::string& schema, const std::string& charset)
{
    if (!connection_)
    {
        connection_ = mysql_init(nullptr);
        if (!connection_)
        {
            return false;
        }
    }

    mysql_options(connection_, MYSQL_SET_CHARSET_NAME, charset.c_str());

    if (!mysql_real_connect(connection_, host.c_str(), user.c_str(), password.c_str(), schema.c_str(), port, nullptr, 0))
    {
        return false;
    }

    return true;
}

void DBManager::Disconnect()
{
    if (connection_)
    {
        mysql_close(connection_);
        connection_ = nullptr;
    }
}

