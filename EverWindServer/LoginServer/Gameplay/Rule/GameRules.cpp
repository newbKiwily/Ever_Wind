#include "GameRules.h"
#include <fstream>
#include <sstream>
#include <stdexcept>
namespace EverWind::Gameplay
{
void GameRules::Initialize(const std::string &path)
{
    if (initialized)
        throw std::logic_error("Game rules already initialized");
    std::ifstream input(path);
    if (!input)
        throw std::runtime_error("Gameplay rules missing: " + path);
    sourcePath = path;
    std::string line;
    while (std::getline(input, line))
    {
        std::istringstream s(line);
        std::string type;
        if (!(s >> type) || type[0] == '#')
            continue;
        int id;
        if (type == "map" || type == "spawn" || type == "surface")
            mapRows.push_back(line);
        else if (type == "monster" || type == "resource" || type == "resourcecfg" || type == "box" ||
                 type == "zone")
            objectRows.push_back(line);
        else if (type == "skill")
        {
            Skill value{};
            s >> id >> value.duration >> value.hit >> value.reach >> value.damage >> value.cooldown >>
                value.push;
            skills.emplace(id, value);
        }
        else if (type == "recipe")
        {
            Recipe value{};
            int count = 0;
            s >> id >> value.duration >> value.output >> value.count >> count;
            for (int i = 0; i < count; ++i)
            {
                int key = 0, amount = 0;
                s >> key >> amount;
                value.inputs.emplace_back(key, amount);
            }
            recipes.emplace(id, std::move(value));
        }
        else if (type == "item")
        {
            s >> id;
            s >> items[id].key;
        }
        else if (type == "itemdef")
        {
            s >> id;
            auto &v = items[id];
            s >> v.kind >> v.slot >> v.power >> v.heal >> v.gatherKey >> v.craftKey;
        }
        else if (type == "skillmeta")
        {
            s >> id;
            s >> skills.at(id).ratio >> skills.at(id).area;
        }
        else if (type == "policy")
        {
            production = true;
            s >> reviveDelay >> monsterRefill;
        }
        else if (type == "tutorialmove")
        {
            s >> tutorialMove.x >> tutorialMove.y >> tutorialMove.z >> tutorialMoveRadius;
        }
        else if (type == "questcondition")
        {
            s >> id;
            auto &q = quests[id];
            q.id = id;
            QuestCondition c;
            s >> c.kind >> c.target >> c.required;
            q.conditions.push_back(c);
        }
        else if (type == "questreward")
        {
            int item = 0, amount = 0;
            s >> id >> item >> amount;
            quests[id].id = id;
            quests[id].rewards.emplace_back(item, amount);
        }
        else
            throw std::runtime_error("Unknown rules row: " + type);
        if (s.fail())
            throw std::runtime_error("Malformed rules row: " + line);
    }
    if (skills.empty() || mapRows.empty())
        throw std::runtime_error("Incomplete gameplay rules");
    initialized = true;
}
} // namespace EverWind::Gameplay
