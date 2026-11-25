#pragma once
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <stdexcept>
#include <type_traits>
namespace NetPackets
{
enum class PacketId : uint16_t
{
    C2S_JOIN_REQUEST = 0x601,
    C2S_MOVE_REQUEST = 0x602,
    C2S_MODE_REQUEST = 0x603,
    C2S_ATTACK_REQUEST = 0x604,
    C2S_GATHER_REQUEST = 0x605,
    C2S_CRAFT_REQUEST = 0x606,
    C2S_CANCEL_REQUEST = 0x607,
    C2S_CHANGE_MAP_REQUEST = 0x608,
    C2S_REVIVE_REQUEST = 0x609,
    C2S_PROBE_REQUEST = 0x60a,
    C2S_USE_ITEM_REQUEST = 0x60b,
    C2S_EQUIP_REQUEST = 0x60c,
    C2S_UNEQUIP_REQUEST = 0x60d,
    C2S_CLAIM_QUEST_REQUEST = 0x60e,
    C2S_TUTORIAL_REQUEST = 0x60f,
    C2S_RESOURCE_REFILL_REQUEST = 0x610,
    C2S_SAVE_REQUEST = 0x611,
    C2S_LOGOUT_REQUEST = 0x612,
    C2S_ATTACK_TARGETS_REQUEST = 0x613,
    C2S_UPGRADE_STAT_REQUEST = 0x614,
    C2S_UPGRADE_SKILL_REQUEST = 0x615,
    C2S_RESET_STATS_REQUEST = 0x616,
    C2S_RESET_SKILLS_REQUEST = 0x617,
    S2C_ACK = 0x701,
    S2C_WELCOME = 0x702,
    S2C_SPAWN = 0x703,
    S2C_DESPAWN = 0x704,
    S2C_STATE = 0x705,
    S2C_MOVE_RESULT = 0x706,
    S2C_ACTION_STARTED = 0x707,
    S2C_ACTION_ENDED = 0x708,
    S2C_ACTION_CANCELLED = 0x709,
    S2C_HIT = 0x70a,
    S2C_INVENTORY = 0x70b,
    S2C_ZONE = 0x70c,
    S2C_SNAPSHOT_END = 0x70d,
    S2C_STATS = 0x70e,
    S2C_EQUIPMENT = 0x70f,
    S2C_QUEST = 0x710,
    S2C_TUTORIAL = 0x711,
    S2C_SAVED = 0x712,
    S2C_GROWTH = 0x713,
    S2C_SKILL_RANK = 0x714,
    C2S_LOGIN_REQ = 0x100,
    S2C_LOGIN_ACK = 0x101,
    C2S_SIGNUP_REQ = 0x102,
    S2C_SIGNUP_ACK = 0x103,
    C2S_MOVESYNC_REQ = 0x104,
    SC2_MOVESYNC_ACK = 0x105,
    S2C_PLAYERLIST_ACK = 0x106,
    S2C_PLAYERLIST_RUNTIME = 0X107,
    S2C_LOGOUT_ACK = 0x114,
    _INVENTORYITEM = 0x115,
    _PLAYERSTAT = 0X116,
    S2C_ENEMY_SPAWN = 0x118,
    C2S_ATTACK_REQ = 0x119,
    S2C_ENEMY_DAMAGED = 0x120,
    C2S_ENEMY_MOVE_SYNC = 0x121,
    S2C_ENEMY_MOVE_SYNC = 0x122,
    C2S_ENEMY_ATTACK_ANIM = 0x123,
    S2C_ENEMY_ATTACK_ANIM = 0x124,
    _ONESHOT_ANIM_SYNC = 0x125,
    _INTERACT_SYNC = 0x127,
    _DEAD_SYNC = 0x130,
    _COMBAT_STATE_SYNC = 0X131,
    C2S_MAP_CHANGE_REQ = 0X132,
    S2C_MAP_CHANGE_ACK = 0X133,
    C2S_ENEMY_DEAD_REQ = 0X134,
    S2C_ENEMY_DEAD_ACK = 0X135,
    S2C_QUEST_INFO = 0X136,
    C2S_QUEST_RESET = 0X137,
    C2S_QUEST_SAVE = 0X138
};
}

namespace NetPackets
{
inline constexpr uint16_t MAX_PACKET_SIZE = 1024;
inline constexpr uint16_t MIN_PACKET_SIZE = sizeof(uint16_t) * 2;

inline constexpr int MAX_QUEST_PROGRESS_COUNT = 8;
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PacketHeader
{
    uint16_t Length;
    // 헤더를 포함한 길이
    uint16_t Id;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_C2S_ATTACK_REQ
{
    int32_t instanceId;

    float damage;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_C2S_ENEMY_DEAD_REQ
{
    int32_t instanceId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_COMBAT_STATE_SYNC
{
    int32_t UserDBID;

    uint8_t isCombat;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_DEAD_SYNC
{
    int32_t UserDBID;

    uint8_t isStart;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_ENEMY_ATTACK_ANIM
{
    int32_t instanceId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_ENEMY_MOVE_SYNC
{
    int32_t instanceId;

    float posX, posY, posZ;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_ENEMY_SPAWN
{
    int32_t instanceId;
    int32_t enemyId;

    float posX, posY, posZ;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_INTERACT_SYNC
{
    int32_t UserDBID;

    uint8_t isStart;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_INVENTORY_ITEM
{
    char ItemId[32];

    int32_t amount;
    int32_t slotIndex;
    int32_t isEquipped;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_LOGIN_ACK
{
    int32_t ResultCode;
    int32_t UserDBID;
    int32_t UserMapId;

    float PosX;
    float PosY;
    float PosZ;

    int32_t TutorialStep;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_LOGIN_REQ
{
    char UserID[32];
    char Password[64];
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_LOGOUT_ACK
{
    int32_t UserDBID;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_MAP_CHANGE_ACK
{
    int32_t MapId;

    float PosX;
    float PosY;
    float PosZ;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_MAP_CHANGE_REQ
{
    int32_t UserDBID;
    int32_t TargetMapId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_MOVE_SYNC
{
    int32_t UserDBID;

    float PosX, PosY, PosZ;
    float Speed;

    uint32_t Timestamp;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_ONESHOT_ANIM_SYNC
{
    int32_t UserDBID;
    int32_t AnimCode;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_PLAYER_LIST_ACK
{
    int32_t UserDBID;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_PLAYER_LIST_RUNTIME_ACK
{
    int32_t UserDBID;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_QUEST_DATA
{
    int32_t questId;
    int32_t isCompleted;
    int32_t rewardClaimed;
    int32_t conditionCount;
    int32_t currentCounts[MAX_QUEST_PROGRESS_COUNT];
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_QUEST_RESET
{
    int32_t userDbId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_S2C_ENEMY_DAMAGED
{
    int32_t instanceId;

    float currentHp;
    float damageAmount;

    int32_t ownerDbId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_S2C_ENEMY_DEAD_ACK
{
    int32_t instanceId;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_SIGNUP_ACK
{
    int32_t ResultCode;
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_SIGNUP_REQ
{
    char UserID[32];
    char Password[64];
};
#pragma pack(pop)
}

namespace NetPackets
{
#pragma pack(push, 1)
struct PKT_USERSTAT
{
    int32_t attack_power;
    int32_t defence_power;

    float hp;
    float max_hp;
    float speed;

    int32_t tutorialStep;
};
#pragma pack(pop)
}

namespace NetPackets
{
void ValidatePacketLength(uint16_t len);

template <typename T>
inline void SecureCopy(char *dest, size_t destSize, const T &src)
{
    static_assert(std::is_trivially_copyable_v<T>, "Packet must be trivially copyable");

    if (sizeof(T) > destSize)
    {
        throw std::runtime_error("SecureCopy destination too small");
    }

    std::memcpy(dest, &src, sizeof(T));
}
}
