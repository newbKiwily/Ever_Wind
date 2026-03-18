#include "Queries.h"
#include "DBManager.h"

#include <vector>
#include <cstring>
#include <mutex>
#include <mysql.h>
#include "Structs.h"

Queries::Queries(DBManager& db)
    : db_(db)
{
}

bool Queries::FetchUser(const std::string& userId, std::string& outUserId, std::string& outPasswordHash, std::string& outSalt, int& outMapId,
    float& outPosX, float& outPosY, float& outPosZ, bool& outFound)
{
    outFound = false;
    MYSQL* connection = db_.GetConnection();
    if (!connection) return false;

    const char* sql = "SELECT A.UserID, A.PasswordHash, A.Salt, I.MapId, "
        "COALESCE(I.PosX, M.SpawnX), COALESCE(I.PosY, M.SpawnY), COALESCE(I.PosZ, M.SpawnZ) "
        "FROM UserAccount A "
        "INNER JOIN UserInfo I ON A.UserID = I.UserID "
        "LEFT JOIN MapInfo M ON I.MapId = M.MapId "
        "WHERE A.UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(connection);
    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) return false;

    MYSQL_BIND bind[1]{};
    unsigned long idLen = userId.length();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (void*)userId.c_str();
    bind[0].buffer_length = idLen;
    bind[0].length = &idLen;

    mysql_stmt_bind_param(stmt, bind);
    mysql_stmt_execute(stmt);

    char szUserId[33]{}, szPwHash[65]{}, szSalt[33]{};
    unsigned long lenId, lenHash, lenSalt;
    MYSQL_BIND result[7]{};

    result[0].buffer_type = MYSQL_TYPE_STRING; result[0].buffer = szUserId; result[0].buffer_length = sizeof(szUserId); result[0].length = &lenId;
    result[1].buffer_type = MYSQL_TYPE_STRING; result[1].buffer = szPwHash; result[1].buffer_length = sizeof(szPwHash); result[1].length = &lenHash;
    result[2].buffer_type = MYSQL_TYPE_STRING; result[2].buffer = szSalt; result[2].buffer_length = sizeof(szSalt); result[2].length = &lenSalt;
    result[3].buffer_type = MYSQL_TYPE_LONG;   result[3].buffer = &outMapId;
    result[4].buffer_type = MYSQL_TYPE_FLOAT;  result[4].buffer = &outPosX;
    result[5].buffer_type = MYSQL_TYPE_FLOAT;  result[5].buffer = &outPosY;
    result[6].buffer_type = MYSQL_TYPE_FLOAT;  result[6].buffer = &outPosZ;

    mysql_stmt_bind_result(stmt, result);
    if (mysql_stmt_fetch(stmt) == 0) {
        outFound = true;
        outUserId = std::string(szUserId, lenId);
        outPasswordHash = std::string(szPwHash, lenHash);
        outSalt = std::string(szSalt, lenSalt);
    }
    mysql_stmt_close(stmt);
    return true;
}

bool Queries::InsertUser(const std::string& userId, const std::string& passwordHash, const std::string& salt, bool& duplicated)
{
    duplicated = false;
    MYSQL* conn = db_.GetConnection();
    if (!conn) return false;

    std::lock_guard<std::mutex> guard(db_.GetMutex());

    const char* sqlAcc = "INSERT INTO UserAccount (UserID, PasswordHash, Salt) VALUES (?, ?, ?)";
    MYSQL_STMT* stmt = mysql_stmt_init(conn);
    mysql_stmt_prepare(stmt, sqlAcc, (unsigned long)strlen(sqlAcc));

    MYSQL_BIND bind[3]{};
    unsigned long idLen = userId.size(), hLen = passwordHash.size(), sLen = salt.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING; bind[0].buffer = (char*)userId.c_str(); bind[0].length = &idLen;
    bind[1].buffer_type = MYSQL_TYPE_STRING; bind[1].buffer = (char*)passwordHash.c_str(); bind[1].length = &hLen;
    bind[2].buffer_type = MYSQL_TYPE_STRING; bind[2].buffer = (char*)salt.c_str(); bind[2].length = &sLen;

    mysql_stmt_bind_param(stmt, bind);
    if (mysql_stmt_execute(stmt) != 0) {
        if (mysql_errno(conn) == 1062) duplicated = true;
        mysql_stmt_close(stmt);
        return duplicated;
    }
    mysql_stmt_close(stmt);

    const char* sqlInfo = "INSERT INTO UserInfo (UserID, PosX, PosY, PosZ) VALUES (?, NULL, NULL, NULL)";
    stmt = mysql_stmt_init(conn);
    mysql_stmt_prepare(stmt, sqlInfo, (unsigned long)strlen(sqlInfo));
    mysql_stmt_bind_param(stmt, &bind[0]);
    mysql_stmt_execute(stmt);
    mysql_stmt_close(stmt);

    return true;
}

bool Queries::UpdateUserPosition(const std::string& userId, int mapId, float x, float y, float z)
{
    MYSQL* conn = db_.GetConnection();
    if (!conn) return false;

    const char* sql = "UPDATE UserInfo SET MapId=?, PosX=?, PosY=?, PosZ=? WHERE UserID=?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(conn);
    mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql));

    MYSQL_BIND bind[5]{};
    unsigned long idLen = userId.size();
    bind[0].buffer_type = MYSQL_TYPE_LONG;  bind[0].buffer = &mapId;
    bind[1].buffer_type = MYSQL_TYPE_FLOAT; bind[1].buffer = &x;
    bind[2].buffer_type = MYSQL_TYPE_FLOAT; bind[2].buffer = &y;
    bind[3].buffer_type = MYSQL_TYPE_FLOAT; bind[3].buffer = &z;
    bind[4].buffer_type = MYSQL_TYPE_STRING; bind[4].buffer = (char*)userId.c_str(); bind[4].length = &idLen;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::FetchInventory(const std::string& userId, std::vector<GameStruct::InventoryItem>& outItems)
{
    outItems.clear();
    MYSQL* connection = db_.GetConnection();
    if (!connection) return false;

    const char* sql = "SELECT ItemID, Amount, SlotIndex, IsEquipped FROM Inventory WHERE UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(connection);
    if (!stmt) return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[1]{};
    unsigned long userIdLength = (unsigned long)userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char*)userId.c_str();
    bind[0].buffer_length = userIdLength;
    bind[0].length = &userIdLength;

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0) {
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

    while (mysql_stmt_fetch(stmt) == 0) {
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
bool Queries::FetchAllMapData(std::vector<MapInitialInfo>& outMaps)
{
    outMaps.clear();
    MYSQL* connection = db_.GetConnection();
    if (!connection) return false;

    // MapInfo와 Monster 테이블 조인
    const char* sql = "SELECT M.MapId, M.SpawnEnemyX, M.SpawnEnemyY, M.SpawnEnemyZ, "
        "M.SpawnEnemyRadius, M.MaxEnemyCount, Mo.MonsterId "
        "FROM MapInfo M "
        "LEFT JOIN Monster Mo ON M.MapId = Mo.MapId "
        "ORDER BY M.MapId";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(connection);
    if (!stmt) return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    if (mysql_stmt_execute(stmt) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    int mapId, maxEnemyCount;
    float sx, sy, sz, radius;
    int monsterId;
    my_bool isNullMonsterId;

    MYSQL_BIND resultBind[7]{};
    resultBind[0].buffer_type = MYSQL_TYPE_LONG;  resultBind[0].buffer = &mapId;
    resultBind[1].buffer_type = MYSQL_TYPE_FLOAT; resultBind[1].buffer = &sx;
    resultBind[2].buffer_type = MYSQL_TYPE_FLOAT; resultBind[2].buffer = &sy;
    resultBind[3].buffer_type = MYSQL_TYPE_FLOAT; resultBind[3].buffer = &sz;
    resultBind[4].buffer_type = MYSQL_TYPE_FLOAT; resultBind[4].buffer = &radius;
    resultBind[5].buffer_type = MYSQL_TYPE_LONG;  resultBind[5].buffer = &maxEnemyCount;
    resultBind[6].buffer_type = MYSQL_TYPE_LONG;  resultBind[6].buffer = &monsterId;
    resultBind[6].is_null = &isNullMonsterId;

    mysql_stmt_bind_result(stmt, resultBind);

    int currentMapId = -1;
    MapInitialInfo currentInfo;

    while (mysql_stmt_fetch(stmt) == 0) {
        if (mapId != currentMapId) {
            // 이전 맵 정보 저장
            if (currentMapId != -1) {
                outMaps.push_back(currentInfo);
            }
            // 새 맵 정보 초기화
            currentMapId = mapId;
            currentInfo.mapId = mapId;
            currentInfo.enemySpawnX = sx;
            currentInfo.enemySpawnY = sy;
            currentInfo.enemySpawnZ = sz;
            currentInfo.radius = radius;
            currentInfo.maxEnemyCount = maxEnemyCount;
            currentInfo.enemyIdList.clear(); // MapInitialInfo 구조체에 이 필드가 추가되어야 함
        }

        // 몬스터 ID가 NULL이 아니면 추가
        if (!isNullMonsterId) {
            currentInfo.enemyIdList.push_back(monsterId);
        }
    }

    // 마지막 맵 정보 저장
    if (currentMapId != -1) {
        outMaps.push_back(currentInfo);
    }

    mysql_stmt_free_result(stmt);
    mysql_stmt_close(stmt);

    return !outMaps.empty();
}
bool Queries::FetchUserStat(const std::string& userId, int& attack, int& defence, float& speed, float& hp, float& max_hp)
{
    MYSQL* connection = db_.GetConnection();

    const char* sql = "SELECT MaxHP, HP, DefencePower, AttackPower, Speed FROM UserInfo WHERE UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(connection);
    mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql));

    MYSQL_BIND bind[1]{};
    unsigned long idLen = userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING; bind[0].buffer = (char*)userId.c_str(); bind[0].length = &idLen;
    mysql_stmt_bind_param(stmt, bind);
    mysql_stmt_execute(stmt);

    MYSQL_BIND res[5]{};
    res[0].buffer_type = MYSQL_TYPE_FLOAT; res[0].buffer = &max_hp;
    res[1].buffer_type = MYSQL_TYPE_FLOAT; res[1].buffer = &hp;
    res[2].buffer_type = MYSQL_TYPE_LONG;  res[2].buffer = &defence;
    res[3].buffer_type = MYSQL_TYPE_LONG;  res[3].buffer = &attack;
    res[4].buffer_type = MYSQL_TYPE_FLOAT; res[4].buffer = &speed;

    mysql_stmt_bind_result(stmt, res);
    bool ok = (mysql_stmt_fetch(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

bool Queries::UpdateInventory(const std::string& userId, const NetPackets::PKT_INVENTORY_ITEM& item)
{
    MYSQL* conn = db_.GetConnection();
    if (!conn) return false;

    const char* sql = "REPLACE INTO Inventory (UserID, ItemID, Amount, SlotIndex, IsEquipped) "
        "VALUES (?, ?, ?, ?, ?)";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(conn);
    if (!stmt) return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[5]{};
    unsigned long idLen = userId.size();
    unsigned long itemIdLen = strlen(item.ItemId);

    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char*)userId.c_str();
    bind[0].length = &idLen;

    bind[1].buffer_type = MYSQL_TYPE_STRING;
    bind[1].buffer = (char*)item.ItemId;
    bind[1].length = &itemIdLen;

    bind[2].buffer_type = MYSQL_TYPE_LONG;
    bind[2].buffer = (void*)&item.amount;

    bind[3].buffer_type = MYSQL_TYPE_LONG;
    bind[3].buffer = (void*)&item.slotIndex;

    bind[4].buffer_type = MYSQL_TYPE_TINY;
    signed char eq = (signed char)item.isEquipped;
    bind[4].buffer = &eq;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}


bool Queries::FetchMapdata(const std::string& userId, int& outMapId,
    float& outSpawnEnemyX, float& outSpawnEnemyY, float& outSpawnEnemyZ,
    float& outSpawnRadius, int& outMaxEnemyCount, std::vector<int>& outMonsterIds)
{
    outMonsterIds.clear();
    MYSQL* connection = db_.GetConnection();
    if (!connection) return false;

    // UserInfo, MapInfo, Monster 테이블을 JOIN하여 맵 정보와 몬스터 ID를 한 번에 조회합니다.
    const char* sql =
        "SELECT M.MapId, M.SpawnEnemyX, M.SpawnEnemyY, M.SpawnEnemyZ, M.SpawnEnemyRadius, M.MaxEnemyCount, Mo.MonsterId "
        "FROM UserInfo U "
        "JOIN MapInfo M ON U.MapId = M.MapId "
        "LEFT JOIN Monster Mo ON M.MapId = Mo.MapId "
        "WHERE U.UserID = ?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(connection);
    if (!stmt) return false;

    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[1]{};
    unsigned long userIdLength = (unsigned long)userId.size();
    bind[0].buffer_type = MYSQL_TYPE_STRING;
    bind[0].buffer = (char*)userId.c_str();
    bind[0].buffer_length = userIdLength;
    bind[0].length = &userIdLength;

    if (mysql_stmt_bind_param(stmt, bind) != 0 || mysql_stmt_execute(stmt) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    // 결과 값을 받을 변수들
    int mapId = 0, maxEnemyCount = 0;
    float sx = 0.0f, sy = 0.0f, sz = 0.0f, radius = 0.0f;
    int monsterId = 0;
    my_bool isNullMonsterId = 0; // LEFT JOIN으로 인해 몬스터가 없는 맵일 경우를 대비한 널 체크

    MYSQL_BIND resultBind[7]{};
    resultBind[0].buffer_type = MYSQL_TYPE_LONG;  resultBind[0].buffer = &mapId;
    resultBind[1].buffer_type = MYSQL_TYPE_FLOAT; resultBind[1].buffer = &sx;
    resultBind[2].buffer_type = MYSQL_TYPE_FLOAT; resultBind[2].buffer = &sy;
    resultBind[3].buffer_type = MYSQL_TYPE_FLOAT; resultBind[3].buffer = &sz;
    resultBind[4].buffer_type = MYSQL_TYPE_FLOAT; resultBind[4].buffer = &radius;
    resultBind[5].buffer_type = MYSQL_TYPE_LONG;  resultBind[5].buffer = &maxEnemyCount;
    resultBind[6].buffer_type = MYSQL_TYPE_LONG;  resultBind[6].buffer = &monsterId;
    resultBind[6].is_null = &isNullMonsterId;

    if (mysql_stmt_bind_result(stmt, resultBind) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    bool foundMap = false;
    while (mysql_stmt_fetch(stmt) == 0) {
        // 첫 번째 행에서 맵의 기본 정보들을 설정합니다.
        if (!foundMap) {
            outMapId = mapId;
            outSpawnEnemyX = sx;
            outSpawnEnemyY = sy;
            outSpawnEnemyZ = sz;
            outSpawnRadius = radius;
            outMaxEnemyCount = maxEnemyCount;
            foundMap = true;
        }
        // 맵에 몬스터 데이터가 존재한다면 벡터에 추가합니다.
        if (!isNullMonsterId) {
            outMonsterIds.push_back(monsterId);
        }
    }

    mysql_stmt_free_result(stmt);
    mysql_stmt_close(stmt);

    return foundMap; // 유저 맵 정보를 성공적으로 찾았으면 true 반환
}

bool Queries::UpdateUserStat(const std::string& userId, const NetPackets::PKT_USERSTAT& stat)
{
    MYSQL* conn = db_.GetConnection();
    if (!conn) return false;

    const char* sql = "UPDATE UserInfo SET AttackPower=?, DefencePower=?, HP=?, MaxHP=?, Speed=? WHERE UserID=?";

    std::lock_guard<std::mutex> guard(db_.GetMutex());
    MYSQL_STMT* stmt = mysql_stmt_init(conn);
    if (mysql_stmt_prepare(stmt, sql, (unsigned long)strlen(sql)) != 0) {
        mysql_stmt_close(stmt);
        return false;
    }

    MYSQL_BIND bind[6]{};
    unsigned long idLen = userId.size();

    bind[0].buffer_type = MYSQL_TYPE_LONG;  bind[0].buffer = (void*)&stat.attack_power;
    bind[1].buffer_type = MYSQL_TYPE_LONG;  bind[1].buffer = (void*)&stat.defence_power;
    bind[2].buffer_type = MYSQL_TYPE_FLOAT; bind[2].buffer = (void*)&stat.hp;
    bind[3].buffer_type = MYSQL_TYPE_FLOAT; bind[3].buffer = (void*)&stat.max_hp;
    bind[4].buffer_type = MYSQL_TYPE_FLOAT; bind[4].buffer = (void*)&stat.speed;
    bind[5].buffer_type = MYSQL_TYPE_STRING; bind[5].buffer = (char*)userId.c_str(); bind[5].length = &idLen;

    mysql_stmt_bind_param(stmt, bind);
    bool ok = (mysql_stmt_execute(stmt) == 0);
    mysql_stmt_close(stmt);
    return ok;
}

