#include "GameplayPersistence.h"
#include "Queries.h"
#include <algorithm>
#include <cstring>
#include <fstream>
#include <sstream>
#include <vector>
using namespace EverWind::Gameplay;
namespace
{
std::string Quote(MYSQL *db, const std::string &value)
{
    std::string escaped(value.size() * 2 + 1, '\0');
    auto n =
        mysql_real_escape_string(db, escaped.data(), value.data(), static_cast<unsigned long>(value.size()));
    escaped.resize(n);
    return "'" + escaped + "'";
}
} // namespace
GameplayPersistence::GameplayPersistence(DBManager &db) : db_(db) {}
void GameplayPersistence::Initialize(const std::unordered_map<int, ItemDefinition> &items)
{
    keys_.clear(); ids_.clear(); slots_.clear();
    for (const auto &[id, item] : items) {
        keys_[id] = item.key; ids_[item.key] = id;
        if (item.kind == 1) slots_[id] = item.slot;
    }
}
AccountState GameplayPersistence::Load(const std::string &user)
{
    Queries query(db_);
    AccountState state;
    std::string id;
    bool found = false;
    if (!query.FetchUser(user, id, state.map, state.position.x, state.position.y,
                         state.position.z, state.tutorial, found, &state.hasPosition) ||
        !found)
        return state;
    if (!query.FetchUserStat(user, state.attack, state.defence, state.speed, state.hp, state.maxHp))
        return state;
    std::vector<GameStruct::InventoryItem> inventory;
    if (!query.FetchInventory(user, inventory))
        return state;
    for (const auto &row : inventory)
    {
        std::string key(row.ItemId, strnlen(row.ItemId, sizeof row.ItemId));
        auto item = ids_.find(key);
        if (item == ids_.end())
            continue;
        if (row.isEquipped && slots_.count(item->second))
            state.equipment[slots_.at(item->second)] = item->second;
        else
            state.inventory[item->second] += std::max(0, row.amount);
    }
    std::vector<QuestSaveData> quests;
    if (!query.FetchUserQuests(user, quests))
        return state;
    for (const auto &q : quests)
        state.quests[q.questId] = {q.currentCounts, q.isCompleted, q.rewardClaimed};
    state.loaded = true;
    return state;
}
bool GameplayPersistence::Save(const std::string &user, const AccountState &state)
{
    std::lock_guard<std::mutex> lock(db_.GetMutex());
    auto db = db_.GetConnection();
    if (!db)
        return false;
    auto uid = Quote(db, user);
    auto execute = [&](const std::string &sql) {
        return mysql_real_query(db, sql.data(), static_cast<unsigned long>(sql.size())) == 0;
    };
    if (mysql_autocommit(db, 0) != 0)
        return false;
    std::ostringstream update;
    update.precision(9);
    update << "UPDATE UserInfo SET MapId=" << state.map << ",PosX=" << state.position.x
           << ",PosY=" << state.position.y << ",PosZ=" << state.position.z << ",HP=" << state.hp
           << ",MaxHP=" << state.maxHp << ",AttackPower=" << state.attack << ",DefencePower=" << state.defence
           << ",Speed=" << state.speed << ",TutorialStep=" << state.tutorial << " WHERE UserID=" << uid;
    bool ok = execute(update.str()) && execute("DELETE FROM Inventory WHERE UserID=" + uid);
    std::vector<std::pair<int, int>> inventory(state.inventory.begin(), state.inventory.end());
    std::sort(inventory.begin(), inventory.end());
    int index = 0;
    auto itemRow = [&](int id, int amount, int equipped) {
        if (amount <= 0 || !keys_.count(id))
            return true;
        return execute("INSERT INTO Inventory(UserID,ItemID,Amount,SlotIndex,IsEquipped) VALUES(" + uid +
                       "," + Quote(db, keys_.at(id)) + "," + std::to_string(amount) + "," +
                       std::to_string(index++) + "," + std::to_string(equipped) + ")");
    };
    for (auto [id, n] : inventory)
        if (ok)
            ok = itemRow(id, n, 0);
    for (auto [slot, id] : state.equipment)
        if (ok)
            ok = itemRow(id, 1, 1);
    if (ok)
        ok = execute("DELETE FROM UserQuestProgress WHERE UserID=" + uid) &&
             execute("DELETE FROM UserQuest WHERE UserID=" + uid);
    for (const auto &[id, q] : state.quests)
        if (ok)
        {
            ok = execute("INSERT INTO UserQuest(UserID,QuestId,IsCompleted,RewardClaimed) VALUES(" + uid +
                         "," + std::to_string(id) + "," + (q.complete ? "1" : "0") + "," +
                         (q.claimed ? "1" : "0") + ")");
            for (size_t i = 0; i < q.counts.size() && ok; ++i)
                ok = execute(
                    "INSERT INTO UserQuestProgress(UserID,QuestId,ConditionIndex,CurrentCount) VALUES(" +
                    uid + "," + std::to_string(id) + "," + std::to_string(i) + "," +
                    std::to_string(q.counts[i]) + ")");
        }
    if (ok)
        ok = mysql_commit(db) == 0;
    else
        mysql_rollback(db);
    if (!ok)
        mysql_rollback(db);
    mysql_autocommit(db, 1);
    return ok;
}
