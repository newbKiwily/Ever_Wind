#pragma once
#include <cstdint>
#include <cstring>
#include <functional>
#include <type_traits>
#include <vector>

namespace EverWind::Gameplay
{
inline constexpr uint16_t RequestPacket = 0x500, EventPacket = 0x501, ProtocolVersion = 1;
}

namespace EverWind::Gameplay
{
enum class ActionKind : uint16_t
{
    None,
    Attack,
    Gather,
    Craft,
    Knockback
};
} // namespace EverWind::Gameplay

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
} // namespace EverWind::Gameplay

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
    Cancelled
};
} // namespace EverWind::Gameplay

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
    Activity
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
enum class Mode : uint16_t
{
    NonCombat,
    Combat,
    Looting,
    Dead
};
} // namespace EverWind::Gameplay

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
    Logout
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
struct Vec3
{
    float x = 0, y = 0, z = 0;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
#pragma pack(push, 1)
struct Header
{
    uint16_t length, id;
};
#pragma pack(pop)
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
#pragma pack(push, 1)
struct Request
{
    uint16_t version = ProtocolVersion;
    RequestKind kind = RequestKind::Join;
    uint64_t requestId = 0;
    uint32_t epoch = 0, revision = 0, sequence = 0;
    int32_t target = 0, definition = 0;
    uint64_t actionId = 0;
    Vec3 position{};
};
static_assert(sizeof(Request) == 52);
#pragma pack(pop)
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
#pragma pack(push, 1)
struct Event
{
    uint16_t version = ProtocolVersion;
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
static_assert(sizeof(Event) == 108);
#pragma pack(pop)
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
using Send = std::function<void(const Event &)>;
}

namespace EverWind::Gameplay
{
template <typename T> std::vector<char> Encode(uint16_t id, const T &value)
{
    static_assert(std::is_trivially_copyable_v<T>);
    Header h{static_cast<uint16_t>(sizeof(Header) + sizeof(T)), id};
    std::vector<char> bytes(h.length);
    std::memcpy(bytes.data(), &h, sizeof h);
    std::memcpy(bytes.data() + sizeof h, &value, sizeof value);
    return bytes;
}
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay
{
enum class PacketCheckPhase
{
    Envelope,
    Contract
};
struct PacketContext
{
    PacketCheckPhase phase = PacketCheckPhase::Envelope;
    uint16_t id = 0;
    const char *payload = nullptr;
    size_t size = 0;
    bool authenticated = false;
    Request request{};
};
} // namespace EverWind::Gameplay
