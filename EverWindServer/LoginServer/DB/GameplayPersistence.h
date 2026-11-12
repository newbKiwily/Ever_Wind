#pragma once
#include "../Gameplay/WorldTypes.h"
#include "DBManager.h"
#include <string>
#include <unordered_map>
class GameplayPersistence
{
  public:
    explicit GameplayPersistence(DBManager &db);
    void Initialize(const std::unordered_map<int, EverWind::Gameplay::ItemDefinition> &items);
    EverWind::Gameplay::AccountState Load(const std::string &user);
    bool Save(const std::string &user, const EverWind::Gameplay::AccountState &state);

  private:
    DBManager &db_;
    std::unordered_map<int, std::string> keys_;
    std::unordered_map<std::string, int> ids_;
    std::unordered_map<int, int> slots_;
};
