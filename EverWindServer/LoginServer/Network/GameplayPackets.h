#pragma once
#include "Packet.h"
#include "../Gameplay/CoreData/Protocol.h"

namespace EverWind::Gameplay
{
template <typename T>
std::vector<char> Encode(uint16_t id, const T &value)
{
    static_assert(std::is_trivially_copyable_v<T>);
    static_assert(sizeof(NetPackets::PacketHeader) + sizeof(T) <= NetPackets::MAX_PACKET_SIZE);

    NetPackets::PacketHeader header{static_cast<uint16_t>(sizeof(NetPackets::PacketHeader) + sizeof(T)), id};

    std::vector<char> bytes(header.Length);

    std::memcpy(bytes.data(), &header, sizeof header);
    std::memcpy(bytes.data() + sizeof header, &value, sizeof value);

    return bytes;
}

// 헤더 ID로 본문 형식을 결정한다. 내부 Request/Event는 전송 구조체가 아니다.
#pragma pack(push, 1)
struct JoinRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(JoinRequestPayload) == 16);

struct MoveRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    Vec3 position;
};
static_assert(sizeof(MoveRequestPayload) == 32);

struct ModeRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(ModeRequestPayload) == 20);

struct AttackRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t target;
    int32_t definition;

    Vec3 position;
};
static_assert(sizeof(AttackRequestPayload) == 36);

struct GatherRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t target;
};
static_assert(sizeof(GatherRequestPayload) == 20);

struct CraftRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(CraftRequestPayload) == 20);

struct CancelRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    uint64_t actionId;
};
static_assert(sizeof(CancelRequestPayload) == 24);

struct ChangeMapRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(ChangeMapRequestPayload) == 20);

struct ReviveRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(ReviveRequestPayload) == 16);

struct ProbeRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t target;
    int32_t definition;
};
static_assert(sizeof(ProbeRequestPayload) == 24);

struct UseItemRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
    int32_t target;

    Vec3 position;
};
static_assert(sizeof(UseItemRequestPayload) == 36);

struct EquipRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(EquipRequestPayload) == 20);

struct UnequipRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(UnequipRequestPayload) == 20);

struct ClaimQuestRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(ClaimQuestRequestPayload) == 20);

struct TutorialRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(TutorialRequestPayload) == 20);

struct ResourceRefillRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t target;

    uint32_t sequence;
};
static_assert(sizeof(ResourceRefillRequestPayload) == 24);

struct SaveRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(SaveRequestPayload) == 16);

struct LogoutRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(LogoutRequestPayload) == 16);

struct AttackTargetsRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    uint64_t actionId;

    int32_t definition;

    uint32_t sequence;

    uint16_t targetCount;

    int32_t targets[MaxAttackTargets];
};
static_assert(sizeof(AttackTargetsRequestPayload) == 290);

struct UpgradeStatRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(UpgradeStatRequestPayload) == 20);

struct UpgradeSkillRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;

    int32_t definition;
};
static_assert(sizeof(UpgradeSkillRequestPayload) == 20);

struct ResetStatsRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(ResetStatsRequestPayload) == 16);

struct ResetSkillsRequestPayload
{
    uint64_t requestId;

    uint32_t epoch;
    uint32_t revision;
};
static_assert(sizeof(ResetSkillsRequestPayload) == 16);

struct AckPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    uint64_t requestId;
    uint64_t actionId;

    Error error;

    uint16_t code;

    Vec3 position;

    Mode mode;
};
static_assert(sizeof(AckPayload) == 58);

struct WelcomePayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    float value;
};
static_assert(sizeof(WelcomePayload) == 70);

struct SpawnPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint16_t code;

    int32_t target;

    float duration;
    float hitTime;
    float value;

    uint16_t flags;
};
static_assert(sizeof(SpawnPayload) == 86);

struct DespawnPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;
};
static_assert(sizeof(DespawnPayload) == 24);

struct StatePayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint16_t code;

    int32_t target;

    float duration;
    float hitTime;
    float value;

    uint16_t flags;
};
static_assert(sizeof(StatePayload) == 86);

struct MoveResultPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint64_t requestId;

    Error error;
};
static_assert(sizeof(MoveResultPayload) == 76);

struct ActionStartedPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint64_t requestId;
    uint64_t actionId;

    uint16_t code;

    int32_t target;

    float duration;
    float hitTime;
    float value;

    uint16_t flags;

    Error error;
};
static_assert(sizeof(ActionStartedPayload) == 104);

struct ActionEndedPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint64_t requestId;
    uint64_t actionId;

    uint16_t code;

    int32_t target;

    float duration;
    float hitTime;
    float value;

    uint16_t flags;

    Error error;
};
static_assert(sizeof(ActionEndedPayload) == 104);

struct ActionCancelledPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    uint64_t requestId;
    uint64_t actionId;

    uint16_t code;

    int32_t target;

    float duration;
    float hitTime;
    float value;

    uint16_t flags;

    Error error;
};
static_assert(sizeof(ActionCancelledPayload) == 104);

struct HitPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    int32_t target;

    uint64_t actionId;
    uint64_t requestId;

    float value;
    float duration;
    float hitTime;

    uint16_t flags;
};
static_assert(sizeof(HitPayload) == 100);

struct InventoryPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    int32_t definition;

    float value;
};
static_assert(sizeof(InventoryPayload) == 32);

struct ZonePayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    int32_t target;

    uint16_t flags;
};
static_assert(sizeof(ZonePayload) == 30);

struct SnapshotEndPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;
};
static_assert(sizeof(SnapshotEndPayload) == 24);

struct StatsPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    Vec3 position;
    Vec3 velocity;

    float hp;
    float maxHp;
    float radius;

    int32_t definition;

    Mode mode;

    int32_t target;

    float value;
    float duration;
};
static_assert(sizeof(StatsPayload) == 78);

struct EquipmentPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    uint16_t code;

    int32_t definition;
};
static_assert(sizeof(EquipmentPayload) == 30);

struct QuestPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    int32_t definition;

    uint16_t code;

    float value;

    uint16_t flags;
};
static_assert(sizeof(QuestPayload) == 36);

struct TutorialPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    int32_t definition;

    uint16_t flags;
};
static_assert(sizeof(TutorialPayload) == 30);

struct SavedPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    uint64_t requestId;

    uint16_t code;

    Error error;
};
static_assert(sizeof(SavedPayload) == 36);

struct GrowthPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    uint16_t code;

    int32_t target;
    int32_t definition;

    float value;
    float radius;

    Vec3 position;
};
static_assert(sizeof(GrowthPayload) == 54);

struct SkillRankPayload
{
    int32_t entity;

    uint32_t epoch;
    uint32_t revision;
    uint32_t sequence;

    double serverTime;

    int32_t definition;

    uint16_t code;

    float value;
    float duration;
    float hitTime;
};
static_assert(sizeof(SkillRankPayload) == 42);

#pragma pack(pop)

inline size_t RequestPayloadSize(uint16_t id)
{
    switch (static_cast<NetPackets::PacketId>(id))
    {
    case NetPackets::PacketId::C2S_JOIN_REQUEST:
        return sizeof(JoinRequestPayload);
    case NetPackets::PacketId::C2S_MOVE_REQUEST:
        return sizeof(MoveRequestPayload);
    case NetPackets::PacketId::C2S_MODE_REQUEST:
        return sizeof(ModeRequestPayload);
    case NetPackets::PacketId::C2S_ATTACK_REQUEST:
        return sizeof(AttackRequestPayload);
    case NetPackets::PacketId::C2S_GATHER_REQUEST:
        return sizeof(GatherRequestPayload);
    case NetPackets::PacketId::C2S_CRAFT_REQUEST:
        return sizeof(CraftRequestPayload);
    case NetPackets::PacketId::C2S_CANCEL_REQUEST:
        return sizeof(CancelRequestPayload);
    case NetPackets::PacketId::C2S_CHANGE_MAP_REQUEST:
        return sizeof(ChangeMapRequestPayload);
    case NetPackets::PacketId::C2S_REVIVE_REQUEST:
        return sizeof(ReviveRequestPayload);
    case NetPackets::PacketId::C2S_PROBE_REQUEST:
        return sizeof(ProbeRequestPayload);
    case NetPackets::PacketId::C2S_USE_ITEM_REQUEST:
        return sizeof(UseItemRequestPayload);
    case NetPackets::PacketId::C2S_EQUIP_REQUEST:
        return sizeof(EquipRequestPayload);
    case NetPackets::PacketId::C2S_UNEQUIP_REQUEST:
        return sizeof(UnequipRequestPayload);
    case NetPackets::PacketId::C2S_CLAIM_QUEST_REQUEST:
        return sizeof(ClaimQuestRequestPayload);
    case NetPackets::PacketId::C2S_TUTORIAL_REQUEST:
        return sizeof(TutorialRequestPayload);
    case NetPackets::PacketId::C2S_RESOURCE_REFILL_REQUEST:
        return sizeof(ResourceRefillRequestPayload);
    case NetPackets::PacketId::C2S_SAVE_REQUEST:
        return sizeof(SaveRequestPayload);
    case NetPackets::PacketId::C2S_LOGOUT_REQUEST:
        return sizeof(LogoutRequestPayload);
    case NetPackets::PacketId::C2S_ATTACK_TARGETS_REQUEST:
        return sizeof(AttackTargetsRequestPayload);
    case NetPackets::PacketId::C2S_UPGRADE_STAT_REQUEST:
        return sizeof(UpgradeStatRequestPayload);
    case NetPackets::PacketId::C2S_UPGRADE_SKILL_REQUEST:
        return sizeof(UpgradeSkillRequestPayload);
    case NetPackets::PacketId::C2S_RESET_STATS_REQUEST:
        return sizeof(ResetStatsRequestPayload);
    case NetPackets::PacketId::C2S_RESET_SKILLS_REQUEST:
        return sizeof(ResetSkillsRequestPayload);
    default:
        return 0;
    }
}

inline bool DecodeRequest(uint16_t id, const char *bytes, size_t size, Request &request)
{
    if (!bytes || !RequestPayloadSize(id) || size != RequestPayloadSize(id))
    {
        return false;
    }

    request = Request{};

    switch (static_cast<NetPackets::PacketId>(id))
    {
    case NetPackets::PacketId::C2S_JOIN_REQUEST:
    {
        JoinRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Join;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    case NetPackets::PacketId::C2S_MOVE_REQUEST:
    {
        MoveRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Move;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.sequence = payload.sequence;

        request.position = payload.position;

        return true;
    }
    case NetPackets::PacketId::C2S_MODE_REQUEST:
    {
        ModeRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Mode;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_ATTACK_REQUEST:
    {
        AttackRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Attack;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.target = payload.target;

        request.definition = payload.definition;

        request.position = payload.position;

        return true;
    }
    case NetPackets::PacketId::C2S_GATHER_REQUEST:
    {
        GatherRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Gather;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.target = payload.target;

        return true;
    }
    case NetPackets::PacketId::C2S_CRAFT_REQUEST:
    {
        CraftRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Craft;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_CANCEL_REQUEST:
    {
        CancelRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Cancel;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.actionId = payload.actionId;

        return true;
    }
    case NetPackets::PacketId::C2S_CHANGE_MAP_REQUEST:
    {
        ChangeMapRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::ChangeMap;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_REVIVE_REQUEST:
    {
        ReviveRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Revive;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    case NetPackets::PacketId::C2S_PROBE_REQUEST:
    {
        ProbeRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Probe;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.target = payload.target;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_USE_ITEM_REQUEST:
    {
        UseItemRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::UseItem;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        request.target = payload.target;

        request.position = payload.position;

        return true;
    }
    case NetPackets::PacketId::C2S_EQUIP_REQUEST:
    {
        EquipRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Equip;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_UNEQUIP_REQUEST:
    {
        UnequipRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Unequip;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_CLAIM_QUEST_REQUEST:
    {
        ClaimQuestRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::ClaimQuest;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_TUTORIAL_REQUEST:
    {
        TutorialRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Tutorial;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_RESOURCE_REFILL_REQUEST:
    {
        ResourceRefillRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::ResourceRefill;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.target = payload.target;

        request.sequence = payload.sequence;

        return true;
    }
    case NetPackets::PacketId::C2S_SAVE_REQUEST:
    {
        SaveRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Save;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    case NetPackets::PacketId::C2S_LOGOUT_REQUEST:
    {
        LogoutRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::Logout;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    case NetPackets::PacketId::C2S_ATTACK_TARGETS_REQUEST:
    {
        AttackTargetsRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::AttackTargets;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.actionId = payload.actionId;

        request.definition = payload.definition;

        request.sequence = payload.sequence;

        request.targetCount = payload.targetCount;
        std::memcpy(request.targets, payload.targets, sizeof request.targets);

        return true;
    }
    case NetPackets::PacketId::C2S_UPGRADE_STAT_REQUEST:
    {
        UpgradeStatRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::UpgradeStat;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_UPGRADE_SKILL_REQUEST:
    {
        UpgradeSkillRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::UpgradeSkill;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        request.definition = payload.definition;

        return true;
    }
    case NetPackets::PacketId::C2S_RESET_STATS_REQUEST:
    {
        ResetStatsRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::ResetStats;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    case NetPackets::PacketId::C2S_RESET_SKILLS_REQUEST:
    {
        ResetSkillsRequestPayload payload;

        std::memcpy(&payload, bytes, sizeof payload);

        request.kind = RequestKind::ResetSkills;

        request.requestId = payload.requestId;

        request.epoch = payload.epoch;

        request.revision = payload.revision;

        return true;
    }
    default:
        return false;
    }
}

inline std::vector<char> EncodeEvent(const Event &event)
{
    switch (event.kind)
    {
    case EventKind::Ack:
    {
        AckPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.requestId = event.requestId;

        payload.actionId = event.actionId;

        payload.error = event.error;

        payload.code = event.code;

        payload.position = event.position;

        payload.mode = event.mode;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_ACK), payload);
    }
    case EventKind::Welcome:
    {
        WelcomePayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.value = event.value;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_WELCOME), payload);
    }
    case EventKind::Spawn:
    {
        SpawnPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.code = event.code;

        payload.target = event.target;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.value = event.value;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_SPAWN), payload);
    }
    case EventKind::Despawn:
    {
        DespawnPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_DESPAWN), payload);
    }
    case EventKind::State:
    {
        StatePayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.code = event.code;

        payload.target = event.target;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.value = event.value;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_STATE), payload);
    }
    case EventKind::MoveResult:
    {
        MoveResultPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.requestId = event.requestId;

        payload.error = event.error;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_MOVE_RESULT), payload);
    }
    case EventKind::ActionStarted:
    {
        ActionStartedPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.requestId = event.requestId;

        payload.actionId = event.actionId;

        payload.code = event.code;

        payload.target = event.target;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.value = event.value;

        payload.flags = event.flags;

        payload.error = event.error;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_ACTION_STARTED), payload);
    }
    case EventKind::ActionEnded:
    {
        ActionEndedPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.requestId = event.requestId;

        payload.actionId = event.actionId;

        payload.code = event.code;

        payload.target = event.target;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.value = event.value;

        payload.flags = event.flags;

        payload.error = event.error;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_ACTION_ENDED), payload);
    }
    case EventKind::ActionCancelled:
    {
        ActionCancelledPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.requestId = event.requestId;

        payload.actionId = event.actionId;

        payload.code = event.code;

        payload.target = event.target;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.value = event.value;

        payload.flags = event.flags;

        payload.error = event.error;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_ACTION_CANCELLED), payload);
    }
    case EventKind::Hit:
    {
        HitPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.target = event.target;

        payload.actionId = event.actionId;

        payload.requestId = event.requestId;

        payload.value = event.value;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_HIT), payload);
    }
    case EventKind::Inventory:
    {
        InventoryPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.definition = event.definition;

        payload.value = event.value;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_INVENTORY), payload);
    }
    case EventKind::Zone:
    {
        ZonePayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.target = event.target;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_ZONE), payload);
    }
    case EventKind::SnapshotEnd:
    {
        SnapshotEndPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_SNAPSHOT_END), payload);
    }
    case EventKind::Stats:
    {
        StatsPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.position = event.position;

        payload.velocity = event.velocity;

        payload.hp = event.hp;

        payload.maxHp = event.maxHp;

        payload.radius = event.radius;

        payload.definition = event.definition;

        payload.mode = event.mode;

        payload.target = event.target;

        payload.value = event.value;

        payload.duration = event.duration;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_STATS), payload);
    }
    case EventKind::Equipment:
    {
        EquipmentPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.code = event.code;

        payload.definition = event.definition;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_EQUIPMENT), payload);
    }
    case EventKind::Quest:
    {
        QuestPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.definition = event.definition;

        payload.code = event.code;

        payload.value = event.value;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_QUEST), payload);
    }
    case EventKind::Tutorial:
    {
        TutorialPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.definition = event.definition;

        payload.flags = event.flags;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_TUTORIAL), payload);
    }
    case EventKind::Saved:
    {
        SavedPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.requestId = event.requestId;

        payload.code = event.code;

        payload.error = event.error;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_SAVED), payload);
    }
    case EventKind::Growth:
    {
        GrowthPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.code = event.code;

        payload.target = event.target;

        payload.definition = event.definition;

        payload.value = event.value;

        payload.radius = event.radius;

        payload.position = event.position;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_GROWTH), payload);
    }
    case EventKind::SkillRank:
    {
        SkillRankPayload payload{};

        payload.entity = event.entity;

        payload.epoch = event.epoch;

        payload.revision = event.revision;

        payload.sequence = event.sequence;

        payload.serverTime = event.serverTime;

        payload.definition = event.definition;

        payload.code = event.code;

        payload.value = event.value;

        payload.duration = event.duration;

        payload.hitTime = event.hitTime;

        return Encode(static_cast<uint16_t>(NetPackets::PacketId::S2C_SKILL_RANK), payload);
    }
    default:
        throw std::invalid_argument("Unknown server packet kind");
    }
}
}
