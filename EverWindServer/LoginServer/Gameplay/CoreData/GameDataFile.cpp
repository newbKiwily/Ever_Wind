#include "GameDataFile.h"
#include <fstream>
#include <iomanip>
#include <sstream>
#include <stdexcept>
#include <unordered_set>
namespace EverWind::Gameplay
{
GameDataHub GameDataFile::Load(const std::string &path)
{
    std::ifstream input(path);

    if (!input)
    {
        throw std::runtime_error("Missing game content file: " + path);
    }

    GameDataHub data;

    data.growth.experienceToNext.fill(0);

    data.growth.skillRanks.clear();

    std::unordered_set<std::string> unique;

    std::unordered_set<int> levels;

    std::unordered_map<int, unsigned> ranks;

    bool format = false, player = false, enabled = false, exit = false;

    std::string line;

    int number = 0;

    while (std::getline(input, line))
    {
        ++number;

        std::istringstream row(line);

        std::string type;

        if (!(row >> type) || type[0] == '#')
        {
            continue;
        }

        try
        {
            auto key = [&](const std::string &id)
            {
                if (!unique.emplace(type + ":" + id).second)
                {
                    throw std::runtime_error("Duplicate definition");
                }
            };

            int id = 0;

            if (type == "format")
            {
                row >> id;

                key("");

                if (id != 1 || number != 1)
                {
                    throw std::runtime_error("Unsupported content format; format 1 must be first");
                }

                format = true;
            }
            else if (!format)
            {
                throw std::runtime_error("Missing content format");
            }
            else if (type == "item")
            {
                ItemDefinition v;

                row >> id >> std::quoted(v.key) >> v.kind >> v.slot >> v.power >> v.heal >> v.gatherKey >> v.craftKey;

                key(std::to_string(id));

                data.items.emplace(id, v);
            }
            else if (type == "skill")
            {
                Skill v{};

                int kind = 0, target = 0;

                row >> id >> v.duration >> v.hit >> v.reach >> v.damage >> v.cooldown >> v.push >> v.ratio >> v.area >> kind >> target;

                if (kind < 0 || kind > 3 || (target != 0 && target != 1))
                {
                    throw std::runtime_error("Invalid skill kind/target flag");
                }

                v.kind = static_cast<SkillKind>(kind);

                v.requiresTarget = target != 0;

                key(std::to_string(id));

                data.skills.emplace(id, v);
            }
            else if (type == "recipe")
            {
                Recipe v{};

                row >> id >> v.duration >> v.output >> v.count;

                key(std::to_string(id));

                data.recipes.emplace(id, v);
            }
            else if (type == "ingredient" || type == "reward")
            {
                int index = 0, item = 0, count = 0;

                row >> id >> index >> item >> count;

                auto &entries = type == "ingredient" ? data.recipes.at(id).inputs : data.quests.at(id).rewards;

                if (index != static_cast<int>(entries.size()))
                {
                    throw std::runtime_error("Non-contiguous entry index");
                }

                entries.emplace_back(item, count);
            }
            else if (type == "quest")
            {
                row >> id;

                key(std::to_string(id));

                data.quests.emplace(id, QuestDefinition{id});
            }
            else if (type == "condition")
            {
                int index = 0;

                QuestCondition v;

                row >> id >> index >> v.kind >> v.target >> v.required;

                auto &entries = data.quests.at(id).conditions;

                if (index != static_cast<int>(entries.size()))
                {
                    throw std::runtime_error("Non-contiguous condition index");
                }

                entries.push_back(v);
            }
            else if (type == "tutorial")
            {
                TutorialDefinition v;

                int move = 0, attack = 0;

                row >> v.id >> v.next >> v.eventKind >> v.target >> v.minimumSeconds >> v.position.x >> v.position.y >> v.position.z >> v.radius >> move >> attack;

                if ((move != 0 && move != 1) || (attack != 0 && attack != 1))
                {
                    throw std::runtime_error("Invalid tutorial flag");
                }

                v.allowMove = move != 0;

                v.allowAttack = attack != 0;

                key(std::to_string(v.id));

                data.tutorials.emplace(v.id, v);
            }
            else if (type == "setting")
            {
                std::string name;

                row >> name >> id;

                key(name);

                if (name == "TutorialEnabled" && (id == 0 || id == 1))
                {
                    data.tutorialEnabled = id != 0;

                    enabled = true;
                }
                else if (name == "TutorialExitMapId" && id >= 0)
                {
                    data.tutorialExitMapId = id;

                    exit = true;
                }
                else
                {
                    throw std::runtime_error("Unknown/invalid setting");
                }
            }
            else if (type == "player")
            {
                key("");

                row >> data.growth.playerAttack >> data.growth.playerDefence;

                player = true;
            }
            else if (type == "level")
            {
                int xp = 0;

                row >> id >> xp;

                if (id < 1 || id > GrowthRules::MaxLevel)
                {
                    throw std::runtime_error("Invalid level");
                }

                key(std::to_string(id));

                levels.emplace(id);

                data.growth.experienceToNext[id - 1] = xp;
            }
            else if (type == "skillrank")
            {
                int rank = 0;

                SkillRankDefinition v;

                row >> id >> rank >> v.multiplier >> v.cooldown;

                if (rank < 1 || rank > GrowthRules::MaxSkillRank)
                {
                    throw std::runtime_error("Invalid rank");
                }

                key(std::to_string(id) + ":" + std::to_string(rank));

                ranks[id] |= 1u << (rank - 1);

                data.growth.skillRanks[id][rank - 1] = v;
            }
            else if (type == "monster")
            {
                MonsterDefinition v;

                row >> id >> v.visual >> v.stats.attack >> v.stats.defence >> v.stats.moveSpeed >> v.hp >> v.radius >> v.experience >> v.attackSkill >> v.attackCooldown;

                key(std::to_string(id));

                data.monsters.emplace(id, v);
            }
            else if (type == "resource")
            {
                ResourceDefinition v;

                row >> id >> v.item >> v.capacity >> v.radius >> v.gatherTime >> v.refillDelay;

                key(std::to_string(id));

                data.resources.emplace(id, v);
            }
            else
            {
                throw std::runtime_error("Unknown row type: " + type);
            }

            if (row.fail())
            {
                throw std::runtime_error("Missing/invalid field");
            }

            row >> std::ws;

            if (!row.eof())
            {
                throw std::runtime_error("Extra field");
            }
        }
        catch (const std::exception &e)
        {
            throw std::runtime_error(path + ":" + std::to_string(number) + ": " + e.what());
        }
    }

    if (!input.eof())
    {
        throw std::runtime_error("Content read failure: " + path);
    }

    if (!format || !player || !enabled || !exit || levels.size() != GrowthRules::MaxLevel)
    {
        throw std::runtime_error("Incomplete content header/settings/growth table: " + path);
    }

    for (const auto &[id, mask] : ranks)
    {
        if (mask != 31 || !data.skills.count(id))
        {
            throw std::runtime_error("Incomplete/unknown skill rank table");
        }
    }

    for (const auto &[id, skill] : data.skills)
    {
        if (skill.kind != SkillKind::Buff && !ranks.count(id))
        {
            throw std::runtime_error("Missing skill rank table");
        }
    }

    data.Validate();

    return data;
}
}
