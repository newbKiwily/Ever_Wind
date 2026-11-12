#include "Queries.h"
#include "DBManager.h"
#include <vector>
#include <memory>
#include <functional>
#include <stdexcept>
#include <cstring>
#include <mutex>
#include <mysql.h>
#include "Structs.h"

Queries::Queries(DBManager &db) : db_(db)
{
}
bool Queries::EnsureMapIds(const std::vector<int> &ids)
{
    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL *connection = db_.GetConnection();
    if (!connection || mysql_query(connection, "START TRANSACTION"))
        return false;
    const char *sql = "INSERT INTO MapInfo(MapId) VALUES(?) ON DUPLICATE KEY UPDATE MapId=VALUES(MapId)";
    std::unique_ptr<MYSQL_STMT, decltype(&mysql_stmt_close)> stmt(mysql_stmt_init(connection),
                                                                  mysql_stmt_close);
    if (!stmt || mysql_stmt_prepare(stmt.get(), sql, static_cast<unsigned long>(strlen(sql))))
    {
        mysql_rollback(connection);
        return false;
    }
    int id = 0;
    MYSQL_BIND input{};
    input.buffer_type = MYSQL_TYPE_LONG;
    input.buffer = &id;
    if (mysql_stmt_bind_param(stmt.get(), &input))
    {
        mysql_rollback(connection);
        return false;
    }
    for (int value : ids)
    {
        id = value;
        if (mysql_stmt_execute(stmt.get()))
        {
            mysql_rollback(connection);
            return false;
        }
    }
    if (mysql_commit(connection))
    {
        mysql_rollback(connection);
        return false;
    }
    return true;
}

bool Queries::FetchUser(const std::string &userId, std::string &outUserId, int &outMapId, float &outPosX,
                        float &outPosY, float &outPosZ, int &outTutorialStep, bool &outFound,
                        bool *outHasPosition)
{
    outFound = false;
    if (outHasPosition)
        *outHasPosition = false;
    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        return false;
    const char *sql =
        "SELECT A.UserID,I.MapId,COALESCE(I.PosX,0),COALESCE(I.PosY,0),COALESCE(I.PosZ,0),I.TutorialStep, "
        "(I.PosX IS NOT NULL AND I.PosY IS NOT NULL AND I.PosZ IS NOT NULL) FROM UserAccount A "
        "INNER JOIN UserInfo I ON A.UserID=I.UserID WHERE A.UserID=?";
    std::unique_ptr<MYSQL_STMT, decltype(&mysql_stmt_close)> stmt(mysql_stmt_init(connection),
                                                                  mysql_stmt_close);
    if (!stmt || mysql_stmt_prepare(stmt.get(), sql, static_cast<unsigned long>(strlen(sql))))
        return false;
    MYSQL_BIND input{};
    unsigned long idLength = static_cast<unsigned long>(userId.size());
    input.buffer_type = MYSQL_TYPE_STRING;
    input.buffer = const_cast<char *>(userId.data());
    input.length = &idLength;
    if (mysql_stmt_bind_param(stmt.get(), &input) || mysql_stmt_execute(stmt.get()))
        return false;
    char id[33]{};
    unsigned long length = 0;
    int hasPosition = 0;
    MYSQL_BIND result[7]{};
    result[0].buffer_type = MYSQL_TYPE_STRING;
    result[0].buffer = id;
    result[0].buffer_length = sizeof id;
    result[0].length = &length;
    result[1].buffer_type = MYSQL_TYPE_LONG;
    result[1].buffer = &outMapId;
    result[2].buffer_type = MYSQL_TYPE_FLOAT;
    result[2].buffer = &outPosX;
    result[3].buffer_type = MYSQL_TYPE_FLOAT;
    result[3].buffer = &outPosY;
    result[4].buffer_type = MYSQL_TYPE_FLOAT;
    result[4].buffer = &outPosZ;
    result[5].buffer_type = MYSQL_TYPE_LONG;
    result[5].buffer = &outTutorialStep;
    result[6].buffer_type = MYSQL_TYPE_LONG;
    result[6].buffer = &hasPosition;
    if (mysql_stmt_bind_result(stmt.get(), result))
        return false;
    int status = mysql_stmt_fetch(stmt.get());
    if (status == MYSQL_NO_DATA)
        return true;
    if (status != 0 || length > sizeof id)
        return false;
    outUserId.assign(id, length);
    outFound = true;
    if (outHasPosition)
        *outHasPosition = hasPosition != 0;
    return true;
}
bool Queries::VerifyPassword(const std::string &userId, const std::string &password, bool &matches)
{
    matches = false;
    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        return false;
    const char *sql = "SELECT 1 FROM UserAccount WHERE UserID=? AND BINARY `Password`=BINARY ?";
    std::unique_ptr<MYSQL_STMT, decltype(&mysql_stmt_close)> stmt(mysql_stmt_init(connection),
                                                                  mysql_stmt_close);
    if (!stmt || mysql_stmt_prepare(stmt.get(), sql, static_cast<unsigned long>(strlen(sql))))
        return false;
    MYSQL_BIND input[2]{};
    unsigned long idLength = static_cast<unsigned long>(userId.size()),
                  pwLength = static_cast<unsigned long>(password.size());
    input[0].buffer_type = MYSQL_TYPE_STRING;
    input[0].buffer = const_cast<char *>(userId.data());
    input[0].length = &idLength;
    input[1].buffer_type = MYSQL_TYPE_STRING;
    input[1].buffer = const_cast<char *>(password.data());
    input[1].length = &pwLength;
    if (mysql_stmt_bind_param(stmt.get(), input) || mysql_stmt_execute(stmt.get()))
        return false;
    int value = 0;
    MYSQL_BIND result{};
    result.buffer_type = MYSQL_TYPE_LONG;
    result.buffer = &value;
    if (mysql_stmt_bind_result(stmt.get(), &result))
        return false;
    int status = mysql_stmt_fetch(stmt.get());
    if (status == MYSQL_NO_DATA)
        return true;
    if (status != 0)
        return false;
    matches = value == 1;
    return true;
}
bool Queries::InsertUser(const std::string &userId, const std::string &password, bool &duplicated)
{
    duplicated = false;
    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        return false;
    // Create credentials and the initial profile together.
    if (mysql_query(connection, "START TRANSACTION"))
        return false;
    const char *sql = "INSERT INTO UserAccount(UserID,`Password`) VALUES(?,?)";
    std::unique_ptr<MYSQL_STMT, decltype(&mysql_stmt_close)> stmt(mysql_stmt_init(connection),
                                                                  mysql_stmt_close);
    if (!stmt || mysql_stmt_prepare(stmt.get(), sql, static_cast<unsigned long>(strlen(sql))))
    {
        mysql_rollback(connection);
        return false;
    }
    MYSQL_BIND input[2]{};
    unsigned long idLength = static_cast<unsigned long>(userId.size()),
                  pwLength = static_cast<unsigned long>(password.size());
    input[0].buffer_type = MYSQL_TYPE_STRING;
    input[0].buffer = const_cast<char *>(userId.data());
    input[0].length = &idLength;
    input[1].buffer_type = MYSQL_TYPE_STRING;
    input[1].buffer = const_cast<char *>(password.data());
    input[1].length = &pwLength;
    if (mysql_stmt_bind_param(stmt.get(), input) || mysql_stmt_execute(stmt.get()))
    {
        duplicated = mysql_stmt_errno(stmt.get()) == 1062;
        mysql_rollback(connection);
        return duplicated;
    }
    const char *profile =
        "INSERT INTO UserInfo(UserID,PosX,PosY,PosZ,TutorialStep) VALUES(?,NULL,NULL,NULL,0)";
    stmt.reset(mysql_stmt_init(connection));
    if (!stmt || mysql_stmt_prepare(stmt.get(), profile, static_cast<unsigned long>(strlen(profile))) ||
        mysql_stmt_bind_param(stmt.get(), input) || mysql_stmt_execute(stmt.get()))
    {
        mysql_rollback(connection);
        return false;
    }
    if (mysql_commit(connection))
    {
        mysql_rollback(connection);
        return false;
    }
    return true;
}

bool Queries::UpdateUserPosition(const std::string &userId, int mapId, float x, float y, float z)
{
    MYSQL *conn = db_.GetConnection();
    if (!conn)
        return false;

    const char *sql = "UPDATE UserInfo SET MapId=?, PosX=?, PosY=?, PosZ=? WHERE UserID=?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(conn);
    mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql));

    MYSQL_BIND bind[5]{};
    unsigned long idLen = userId.size();
    bind[0].buffer_type = MYSQL_TYPE_LONG;
    bind[0].buffer = &mapId;
    bind[1].buffer_type = MYSQL_TYPE_FLOAT;
    bind[1].buffer = &x;
    bind[2].buffer_type = MYSQL_TYPE_FLOAT;
    bind[2].buffer = &y;
    bind[3].buffer_type = MYSQL_TYPE_FLOAT;
    bind[3].buffer = &z;
    bind[4].buffer_type = MYSQL_TYPE_STRING;
    bind[4].buffer = (char *)userId.c_str();
    bind[4].length = &idLen;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::FetchInventory(const std::string &userId, std::vector<GameStruct::InventoryItem> &outItems)
{
    outItems.clear();
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        return false;

    const char *sql = "SELECT ItemID, Amount, SlotIndex, IsEquipped FROM Inventory WHERE UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(connection);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[1]{};
    unsigned long userIdLength = (unsigned long)userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char *)userId.c_str();
    bind[0].buffer_length = userIdLength;
    bind[0].length = &userIdLength;

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    char itemIdBuf[65]{};
    int amount = 0, slotIndex = 0;
    signed char isEquipped = 0;

    MYSQL_BIND resultBind[4]{};
    resultBind[0].buffer_type = MYSQL_TYPE_STRING;
    resultBind[0].buffer = itemIdBuf;
    resultBind[0].buffer_length = sizeof(itemIdBuf);

    resultBind[1].buffer_type = MYSQL_TYPE_LONG;
    resultBind[1].buffer = &amount;

    resultBind[2].buffer_type = MYSQL_TYPE_LONG;
    resultBind[2].buffer = &slotIndex;

    resultBind[3].buffer_type = MYSQL_TYPE_TINY;
    resultBind[3].buffer = &isEquipped;

    mysql_stmt_bind_result(stmt, resultBind);

    while (mysql_stmt_fetch(stmt) == 0)
    {
        GameStruct::InventoryItem item;
        std::memset(item.ItemId, 0, sizeof(item.ItemId));
        std::memcpy(item.ItemId, itemIdBuf, sizeof(item.ItemId));

        item.amount = amount;
        item.slotIndex = slotIndex;
        item.isEquipped = (int)isEquipped;
        outItems.push_back(item);
    }

    mysql_stmt_free_result(stmt);
    mysql_stmt_close(stmt);
    return true;
}

bool Queries::FetchUserQuests(const std::string &userId, std::vector<QuestSaveData> &outQuests)
{
    outQuests.clear();
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        return false;

    const char *sql = "SELECT Q.QuestId, Q.IsCompleted, Q.RewardClaimed, P.ConditionIndex, P.CurrentCount "
                      "FROM UserQuest Q "
                      "LEFT JOIN UserQuestProgress P ON Q.UserID = P.UserID AND Q.QuestId = P.QuestId "
                      "WHERE Q.UserID = ? "
                      "ORDER BY Q.QuestId, P.ConditionIndex";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(connection);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[1]{};
    unsigned long userIdLength = (unsigned long)userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char *)userId.c_str();
    bind[0].buffer_length = userIdLength;
    bind[0].length = &userIdLength;

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    int questId = 0;
    signed char isCompleted = 0;
    signed char rewardClaimed = 0;
    int conditionIndex = 0;
    int currentCount = 0;
    my_bool isNullConditionIndex = 0;
    my_bool isNullCurrentCount = 0;

    MYSQL_BIND resultBind[5]{};
    resultBind[0].buffer_type = MYSQL_TYPE_LONG;
    resultBind[0].buffer = &questId;
    resultBind[1].buffer_type = MYSQL_TYPE_TINY;
    resultBind[1].buffer = &isCompleted;
    resultBind[2].buffer_type = MYSQL_TYPE_TINY;
    resultBind[2].buffer = &rewardClaimed;
    resultBind[3].buffer_type = MYSQL_TYPE_LONG;
    resultBind[3].buffer = &conditionIndex;
    resultBind[3].is_null = &isNullConditionIndex;
    resultBind[4].buffer_type = MYSQL_TYPE_LONG;
    resultBind[4].buffer = &currentCount;
    resultBind[4].is_null = &isNullCurrentCount;

    if (mysql_stmt_bind_result(stmt, resultBind) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    int currentQuestId = -1;
    QuestSaveData currentQuest;

    while (mysql_stmt_fetch(stmt) == 0)
    {
        if (questId != currentQuestId)
        {
            if (currentQuestId != -1)
            {
                outQuests.push_back(currentQuest);
            }

            currentQuestId = questId;
            currentQuest = QuestSaveData{};
            currentQuest.questId = questId;
            currentQuest.isCompleted = isCompleted != 0;
            currentQuest.rewardClaimed = rewardClaimed != 0;
        }

        if (!isNullConditionIndex && !isNullCurrentCount)
        {
            if ((int)currentQuest.currentCounts.size() <= conditionIndex)
            {
                currentQuest.currentCounts.resize(conditionIndex + 1, 0);
            }
            currentQuest.currentCounts[conditionIndex] = currentCount;
        }
    }

    if (currentQuestId != -1)
    {
        outQuests.push_back(currentQuest);
    }

    mysql_stmt_free_result(stmt);
    mysql_stmt_close(stmt);
    return true;
}

bool Queries::FetchUserStat(const std::string &userId, int &attack, int &defence, float &speed, float &hp,
                            float &max_hp)
{
    MYSQL *connection = db_.GetConnection();

    const char *sql = "SELECT MaxHP, HP, DefencePower, AttackPower, Speed FROM UserInfo WHERE UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(connection);
    mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql));

    MYSQL_BIND bind[1]{};
    unsigned long idLen = userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char *)userId.c_str();
    bind[0].length = &idLen;
    mysql_stmt_bind_param(stmt, bind);
    mysql_stmt_execute(stmt);

    MYSQL_BIND res[5]{};
    res[0].buffer_type = MYSQL_TYPE_FLOAT;
    res[0].buffer = &max_hp;
    res[1].buffer_type = MYSQL_TYPE_FLOAT;
    res[1].buffer = &hp;
    res[2].buffer_type = MYSQL_TYPE_LONG;
    res[2].buffer = &defence;
    res[3].buffer_type = MYSQL_TYPE_LONG;
    res[3].buffer = &attack;
    res[4].buffer_type = MYSQL_TYPE_FLOAT;
    res[4].buffer = &speed;

    mysql_stmt_bind_result(stmt, res);
    bool ok = (mysql_stmt_fetch(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::UpdateInventory(const std::string &userId, const NetPackets::PKT_INVENTORY_ITEM &item)
{
    MYSQL *conn = db_.GetConnection();
    if (!conn)
        return false;

    const char *sql = "REPLACE INTO Inventory (UserID, ItemID, Amount, SlotIndex, IsEquipped) "
                      "VALUES (?, ?, ?, ?, ?)";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(conn);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[5]{};
    unsigned long idLen = userId.size();
    unsigned long itemIdLen = strlen(item.ItemId);

    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char *)userId.c_str();
    bind[0].length = &idLen;

    bind[1].buffer_type = MYSQL_TYPE_STRING;
    bind[1].buffer = (char *)item.ItemId;
    bind[1].length = &itemIdLen;

    bind[2].buffer_type = MYSQL_TYPE_LONG;
    bind[2].buffer = (void *)&item.amount;

    bind[3].buffer_type = MYSQL_TYPE_LONG;
    bind[3].buffer = (void *)&item.slotIndex;

    bind[4].buffer_type = MYSQL_TYPE_TINY;
    signed char eq = (signed char)item.isEquipped;
    bind[4].buffer = &eq;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::UpdateUserStat(const std::string &userId, const NetPackets::PKT_USERSTAT &stat)
{
    MYSQL *conn = db_.GetConnection();
    if (!conn)
        return false;

    const char *sql = "UPDATE UserInfo SET AttackPower=?, DefencePower=?, HP=?, MaxHP=?, Speed=?, "
                      "TutorialStep=? WHERE UserID=?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT *stmt = mysql_stmt_init(conn);
    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[7]{};
    unsigned long idLen = userId.size();

    bind[0].buffer_type = MYSQL_TYPE_LONG;
    bind[0].buffer = (void *)&stat.attack_power;
    bind[1].buffer_type = MYSQL_TYPE_LONG;
    bind[1].buffer = (void *)&stat.defence_power;
    bind[2].buffer_type = MYSQL_TYPE_FLOAT;
    bind[2].buffer = (void *)&stat.hp;
    bind[3].buffer_type = MYSQL_TYPE_FLOAT;
    bind[3].buffer = (void *)&stat.max_hp;
    bind[4].buffer_type = MYSQL_TYPE_FLOAT;
    bind[4].buffer = (void *)&stat.speed;
    bind[5].buffer_type = MYSQL_TYPE_LONG;
    bind[5].buffer = (void *)&stat.tutorialStep;
    bind[6].buffer_type = MYSQL_TYPE_STRING;
    bind[6].buffer = (char *)userId.c_str();
    bind[6].length = &idLen;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::ResetUserQuests(const std::string &userId)
{
    MYSQL *conn = db_.GetConnection();
    if (!conn)
        return false;

    std::lock_guard<std::mutex> guard(db_.GetMutex());

    const char *deleteProgressSql = "DELETE FROM UserQuestProgress WHERE UserID=?";
    MYSQL_STMT *stmt = mysql_stmt_init(conn);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, deleteProgressSql, (unsigned long)strlen(deleteProgressSql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[1]{};
    unsigned long idLen = (unsigned long)userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char *)userId.c_str();
    bind[0].length = &idLen;

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }
    mysql_stmt_close(stmt);

    const char *deleteQuestSql = "DELETE FROM UserQuest WHERE UserID=?";
    stmt = mysql_stmt_init(conn);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, deleteQuestSql, (unsigned long)strlen(deleteQuestSql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    mysql_stmt_close(stmt);
    return true;
}

bool Queries::UpdateUserQuest(const std::string &userId, const NetPackets::PKT_QUEST_DATA &quest)
{
    MYSQL *conn = db_.GetConnection();
    if (!conn)
        return false;

    std::lock_guard<std::mutex> guard(db_.GetMutex());

    const char *questSql = "REPLACE INTO UserQuest (UserID, QuestId, IsCompleted, RewardClaimed) "
                           "VALUES (?, ?, ?, ?)";

    MYSQL_STMT *stmt = mysql_stmt_init(conn);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, questSql, (unsigned long)strlen(questSql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    unsigned long idLen = (unsigned long)userId.size();
    signed char isCompleted = (signed char)quest.isCompleted;
    signed char rewardClaimed = (signed char)quest.rewardClaimed;

    MYSQL_BIND questBind[4]{};
    questBind[0].buffer_type = MYSQL_TYPE_STRING;
    questBind[0].buffer = (char *)userId.c_str();
    questBind[0].length = &idLen;
    questBind[1].buffer_type = MYSQL_TYPE_LONG;
    questBind[1].buffer = (void *)&quest.questId;
    questBind[2].buffer_type = MYSQL_TYPE_TINY;
    questBind[2].buffer = &isCompleted;
    questBind[3].buffer_type = MYSQL_TYPE_TINY;
    questBind[3].buffer = &rewardClaimed;

    if (mysql_stmt_bind_param(stmt, questBind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }
    mysql_stmt_close(stmt);

    const char *deleteProgressSql = "DELETE FROM UserQuestProgress WHERE UserID=? AND QuestId=?";
    stmt = mysql_stmt_init(conn);
    if (!stmt)
        return false;

    if (mysql_stmt_prepare(stmt, deleteProgressSql, (unsigned long)strlen(deleteProgressSql)) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND deleteBind[2]{};
    deleteBind[0].buffer_type = MYSQL_TYPE_STRING;
    deleteBind[0].buffer = (char *)userId.c_str();
    deleteBind[0].length = &idLen;
    deleteBind[1].buffer_type = MYSQL_TYPE_LONG;
    deleteBind[1].buffer = (void *)&quest.questId;

    if (mysql_stmt_bind_param(stmt, deleteBind) != 0 || mysql_stmt_execute(stmt) != 0)
    {
        mysql_stmt_close(stmt);
        return false;
    }
    mysql_stmt_close(stmt);

    const char *progressSql = "INSERT INTO UserQuestProgress (UserID, QuestId, ConditionIndex, CurrentCount) "
                              "VALUES (?, ?, ?, ?)";

    for (int i = 0; i < quest.conditionCount && i < NetPackets::MAX_QUEST_PROGRESS_COUNT; ++i)
    {
        stmt = mysql_stmt_init(conn);
        if (!stmt)
            return false;

        if (mysql_stmt_prepare(stmt, progressSql, (unsigned long)strlen(progressSql)) != 0)
        {
            mysql_stmt_close(stmt);
            return false;
        }

        int conditionIndex = i;
        int currentCount = quest.currentCounts[i];

        MYSQL_BIND progressBind[4]{};
        progressBind[0].buffer_type = MYSQL_TYPE_STRING;
        progressBind[0].buffer = (char *)userId.c_str();
        progressBind[0].length = &idLen;
        progressBind[1].buffer_type = MYSQL_TYPE_LONG;
        progressBind[1].buffer = (void *)&quest.questId;
        progressBind[2].buffer_type = MYSQL_TYPE_LONG;
        progressBind[2].buffer = &conditionIndex;
        progressBind[3].buffer_type = MYSQL_TYPE_LONG;
        progressBind[3].buffer = &currentCount;

        if (mysql_stmt_bind_param(stmt, progressBind) != 0 || mysql_stmt_execute(stmt) != 0)
        {
            mysql_stmt_close(stmt);
            return false;
        }

        mysql_stmt_close(stmt);
    }

    return true;
}

EverWind::Gameplay::GameRules Queries::LoadGameDefinitions()
{
    using namespace EverWind::Gameplay;
    std::lock_guard<std::mutex> lock(db_.GetMutex());
    MYSQL *connection = db_.GetConnection();
    if (!connection)
        throw std::runtime_error("Database unavailable during game initialization");
    GameRules data;
    auto rows = [&](const char *sql, auto consume) {
        if (mysql_query(connection, sql))
            throw std::runtime_error(std::string("Game data query failed: ") + mysql_error(connection));
        std::unique_ptr<MYSQL_RES, decltype(&mysql_free_result)> result(mysql_store_result(connection),
                                                                        mysql_free_result);
        if (!result)
            throw std::runtime_error("Missing game data query result");
        while (auto row = mysql_fetch_row(result.get()))
        {
            std::vector<std::string> values;
            for (unsigned i = 0; i < mysql_num_fields(result.get()); ++i)
            {
                if (!row[i])
                    throw std::runtime_error("NULL in game definition");
                values.emplace_back(row[i]);
            }
            consume(values);
        }
        if (mysql_errno(connection))
            throw std::runtime_error("Game data fetch failed");
    };
    if (mysql_query(connection, "START TRANSACTION WITH CONSISTENT SNAPSHOT"))
        throw std::runtime_error("Game data snapshot failed");
    try
    {
        rows("SELECT ItemID,ItemKey,Kind,EquipmentSlot,Power,Heal,GatherEventKey,CraftEventKey FROM "
             "ItemDefinition ORDER BY ItemID",
             [&](const auto &r) {
                 ItemDefinition v;
                 v.key = r[1];
                 v.kind = std::stoi(r[2]);
                 v.slot = std::stoi(r[3]);
                 v.power = std::stoi(r[4]);
                 v.heal = std::stof(r[5]);
                 v.gatherKey = std::stoi(r[6]);
                 v.craftKey = std::stoi(r[7]);
                 data.items.emplace(std::stoi(r[0]), v);
             });
        rows("SELECT SkillID,Duration,ImpactSeconds,Reach,Damage,Cooldown,Knockback,DamageRatio,AreaRadius "
             "FROM SkillDefinition ORDER BY SkillID",
             [&](const auto &r) {
                 Skill v{};
                 v.duration = std::stof(r[1]);
                 v.hit = std::stof(r[2]);
                 v.reach = std::stof(r[3]);
                 v.damage = std::stof(r[4]);
                 v.cooldown = std::stof(r[5]);
                 v.push = std::stof(r[6]);
                 v.ratio = std::stof(r[7]);
                 v.area = std::stof(r[8]);
                 data.skills.emplace(std::stoi(r[0]), v);
             });
        rows("SELECT RecipeID,CraftSeconds,ResultItemID,ResultCount FROM Recipe ORDER BY RecipeID",
             [&](const auto &r) {
                 Recipe v{};
                 v.duration = std::stof(r[1]);
                 v.output = std::stoi(r[2]);
                 v.count = std::stoi(r[3]);
                 data.recipes.emplace(std::stoi(r[0]), v);
             });
        rows("SELECT RecipeID,IngredientIndex,ItemID,RequiredCount FROM RecipeIngredient ORDER BY "
             "RecipeID,IngredientIndex",
             [&](const auto &r) {
                 auto &v = data.recipes.at(std::stoi(r[0]));
                 if (std::stoi(r[1]) != static_cast<int>(v.inputs.size()))
                     throw std::runtime_error("Recipe ingredient index gap");
                 v.inputs.emplace_back(std::stoi(r[2]), std::stoi(r[3]));
             });
        rows("SELECT QuestID FROM QuestDefinition ORDER BY QuestID", [&](const auto &r) {
            int id = std::stoi(r[0]);
            data.quests[id].id = id;
        });
        rows("SELECT QuestID,ConditionIndex,EventKind,TargetID,RequiredCount FROM QuestCondition ORDER BY "
             "QuestID,ConditionIndex",
             [&](const auto &r) {
                 auto &v = data.quests.at(std::stoi(r[0]));
                 if (std::stoi(r[1]) != static_cast<int>(v.conditions.size()))
                     throw std::runtime_error("Quest condition index gap");
                 v.conditions.push_back({std::stoi(r[2]), std::stoi(r[3]), std::stoi(r[4])});
             });
        rows("SELECT QuestID,RewardIndex,ItemID,Amount FROM QuestReward ORDER BY QuestID,RewardIndex",
             [&](const auto &r) {
                 auto &v = data.quests.at(std::stoi(r[0]));
                 if (std::stoi(r[1]) != static_cast<int>(v.rewards.size()))
                     throw std::runtime_error("Quest reward index gap");
                 v.rewards.emplace_back(std::stoi(r[2]), std::stoi(r[3]));
             });
        rows("SELECT "
             "StepID,NextStepID,EventKind,TargetID,MinimumSeconds,TargetX,TargetY,TargetZ,TargetRadius,"
             "AllowMove,AllowAttack FROM TutorialDefinition ORDER BY StepID",
             [&](const auto &r) {
                 TutorialDefinition v;
                 v.id = std::stoi(r[0]);
                 v.next = std::stoi(r[1]);
                 v.eventKind = std::stoi(r[2]);
                 v.target = std::stoi(r[3]);
                 v.minimumSeconds = std::stof(r[4]);
                 v.position = {std::stof(r[5]), std::stof(r[6]), std::stof(r[7])};
                 v.radius = std::stof(r[8]);
                 v.allowMove = std::stoi(r[9]) != 0;
                 v.allowAttack = std::stoi(r[10]) != 0;
                 data.tutorials.emplace(v.id, v);
             });
        bool enabled = false, gate = false;
        rows("SELECT SettingKey,IntValue FROM GameSetting", [&](const auto &r) {
            if (r[0] == "TutorialEnabled")
            {
                data.tutorialEnabled = std::stoi(r[1]) != 0;
                enabled = true;
            }
            if (r[0] == "TutorialExitMapId")
            {
                data.tutorialExitMapId = std::stoi(r[1]);
                gate = true;
            }
        });
        if (!enabled || !gate)
            throw std::runtime_error("Missing DB game settings");
        data.Validate();
        if (mysql_commit(connection))
            throw std::runtime_error("Game data snapshot commit failed");
        return data;
    }
    catch (...)
    {
        mysql_rollback(connection);
        throw;
    }
}
