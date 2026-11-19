#pragma once
#include "Protocol.h"
#include <cstdint>
#include <deque>
#include <functional>
#include <memory>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>
namespace EverWind::Gameplay
{
struct ItemDefinition
{
    std::string key;

    int kind = 0, slot = 0, power = 0, gatherKey = 0, craftKey = 0;

    float heal = 0;
};
}

namespace EverWind::Gameplay
{
struct QuestCondition
{
    int kind = 0, target = 0, required = 0;
};
}

namespace EverWind::Gameplay
{
struct QuestDefinition
{
    int id = 0;

    std::vector<QuestCondition> conditions;

    std::vector<std::pair<int, int>> rewards;
};
}

namespace EverWind::Gameplay
{
struct TutorialDefinition
{
    int id = 0, next = -1, eventKind = -1, target = -1;

    float minimumSeconds = 0, radius = 2;

    Vec3 position{};

    bool allowMove = true, allowAttack = true;
};

struct QuestProgress
{
    std::vector<int> counts;

    bool complete = false, claimed = false;
};
}

namespace EverWind::Gameplay
{
struct Recipe
{
    float duration;

    int output, count;

    std::vector<std::pair<int, int>> inputs;
};
}

namespace EverWind::Gameplay
{
enum class SkillKind : uint16_t
{
    SingleTarget,
    Ranged,
    Area,
    Buff
};

struct Skill
{
    float duration, hit, reach, damage, cooldown, push;
    float ratio = -1, area = 0;

    SkillKind kind = SkillKind::SingleTarget;

    bool requiresTarget = true;
};
}

namespace EverWind::Gameplay
{
struct Action
{
    uint64_t id = 0;

    ActionKind kind = ActionKind::None;

    int target = 0, definition = 0;

    double start = 0, end = 0, impact = 0;

    bool hit = false;
};
}

namespace EverWind::Gameplay
{
struct Box
{
    int id;

    float x, z, hx, hz;
};
}

namespace EverWind::Gameplay
{
struct Zone
{
    int id;

    Vec3 p;

    float radius;
};
}

namespace EverWind::Gameplay
{
struct HeightField
{
    float x = 0, z = 0, step = 1, flat = 0;

    int columns = 0, rows = 0;

    std::vector<float> samples;

    float Sample(float px, float pz) const;
};
}

namespace EverWind::Gameplay
{
struct AccountState
{
    bool loaded = false, hasPosition = false;

    int map = 0, tutorial = 0, attack = 50, defence = 20;

    float hp = 100, maxHp = 100, speed = 4.5f;

    Vec3 position{};

    std::unordered_map<int, int> inventory, equipment;

    std::unordered_map<int, QuestProgress> quests;
};
}

namespace EverWind::Gameplay
{
struct CachedRequest
{
    Request request;

    Event ack;
};
}
