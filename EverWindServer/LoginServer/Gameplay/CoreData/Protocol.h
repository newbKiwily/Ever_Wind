#pragma once
#include <cstdint>
#include <cstring>
#include <functional>
#include <type_traits>
#include <vector>
namespace EverWind::Gameplay
{
inline constexpr uint16_t MaxAttackTargets = 64;
}

namespace EverWind::Gameplay
{
enum class HitReaction : uint16_t
{
    Knockback = 1,
    DamageOnly = 2,
    Hit = 4
};

enum class ActionKind : uint16_t
{
    None,
    Attack,
    Gather,
    Craft,
    Knockback
};
}

namespace EverWind::Gameplay
{
enum class EntityKind : uint16_t
{
    Player = 1,
    Monster,
    Resource,
    Wall,
    Boundary,
    Zone,
    Placement
};
}

namespace EverWind::Gameplay
{
enum class Error : uint16_t
{
    None,
    Unauthenticated,
    BadRequest,
    WrongWorld,
    Stale,
    Dead,
    Restricted,
    OutOfRange,
    MissingTarget,
    Cooldown,
    Busy,
    Exhausted,
    Ingredients,
    Collision,
    Speed,
    DuplicateConflict,
    Expired,
    Cancelled,
    InsufficientPoints,
    RankLimit,
    UnlearnedSkill
};
}

namespace EverWind::Gameplay
{
enum class EventKind : uint16_t
{
    Ack = 1,
    Welcome,
    Spawn,
    Despawn,
    State,
    MoveResult,
    ActionStarted,
    ActionEnded,
    ActionCancelled,
    Hit,
    Inventory,
    Zone,
    SnapshotEnd,
    Stats,
    Equipment,
    Quest,
    Tutorial,
    Saved,
    Growth,
    SkillRank
};
}

namespace EverWind::Gameplay
{
enum class Mode : uint16_t
{
    NonCombat,
    Combat,
    Looting,
    Dead
};
}

namespace EverWind::Gameplay
{
enum class RequestKind : uint16_t
{
    Join = 1,
    Move,
    Mode,
    Attack,
    Gather,
    Craft,
    Cancel,
    ChangeMap,
    Revive,
    Probe,
    UseItem,
    Equip,
    Unequip,
    ClaimQuest,
    Tutorial,
    ResourceRefill,
    Save,
    Logout,
    AttackTargets,
    UpgradeStat,
    UpgradeSkill,
    ResetStats,
    ResetSkills
};
}

namespace EverWind::Gameplay
{
struct Vec3
{
    float x = 0, y = 0, z = 0;
};
}

namespace EverWind::Gameplay
{
struct Request
{
    RequestKind kind = RequestKind::Join;

    uint64_t requestId = 0;

    uint32_t epoch = 0, revision = 0, sequence = 0;

    int32_t target = 0, definition = 0;

    uint64_t actionId = 0;

    Vec3 position{};

    uint16_t targetCount = 0;

    int32_t targets[MaxAttackTargets]{};
};
}

namespace EverWind::Gameplay
{
struct Event
{
    EventKind kind = EventKind::Ack;

    Error error = Error::None;

    uint16_t code = 0;

    int32_t entity = 0, target = 0, definition = 0;

    uint32_t epoch = 0, revision = 0, sequence = 0;

    uint64_t requestId = 0, actionId = 0;

    double serverTime = 0;

    Vec3 position{}, velocity{};

    float hp = 0, maxHp = 0, radius = 0, duration = 0, hitTime = 0, value = 0;

    Mode mode = Mode::NonCombat;

    uint16_t flags = 0;
};
}

namespace EverWind::Gameplay
{
using Send = std::function<void(const Event &)>;
}
