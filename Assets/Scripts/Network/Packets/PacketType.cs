using System.Runtime.InteropServices;
public enum PacketType : ushort
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
    S2C_MOVESYNC_ACK = 0x105,

    S2C_PLAYERLIST_ACK = 0x106,
    S2C_PLAYERLIST_RUNTIME = 0x107,

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
}

public enum OneshotAnimKey : int
{
    Attack1 = 1,
    Attack2 = 2,
    Attack3 = 3,
    Attack4 = 4,
    Attack5 = 5,
    Damaged1 = 11,
    Damaged2 = 12,
    Damaged3 = 13,
    Success = 21
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketHeader
{
    public ushort Length;
    public ushort Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PKT_LOGIN_REQ
{
    public fixed byte UserID[32];
    public fixed byte Password[64];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_LOGIN_ACK
{
    public int ResultCode;
    public int UserDBID;
    public int UserMapId;
    public float PosX;
    public float PosY;
    public float PosZ;
    public int TutorialStep;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PKT_SIGNUP_REQ
{
    public fixed byte UserID[32];
    public fixed byte Password[64];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_SIGNUP_ACK
{
    public int ResultCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_MOVE_SYNC
{
    public int UserDBID;
    public float PosX, PosY, PosZ;
    public float Speed;
    public uint Timestamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_PLAYER_LIST_ACK
{
    public int UserDBID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_PLAYER_LIST_RUNTIME_ACK
{
    public int UserDBID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_LOGOUT_ACK
{
    public int UserDBID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PKT_INVENTORY_ITEM
{
    public fixed byte ItemId[32];
    public int Amount;
    public int SlotIndex;
    public int IsEquipped;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_USERSTAT
{
    public int AttackPower;
    public int DefecnePower;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public int TutorialStep;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_ENEMY_SPAWN
{
    public int InstanceId;
    public int EnemyId;
    public float PosX, PosY, PosZ;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_C2S_ATTACK_REQ
{
    public int InstanceId;
    public float Damage;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_S2C_ENEMY_DAMAGED
{
    public int InstanceId;
    public float CurrentHp;
    public float DamageAmount;
    public int OwnerDbId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_ENEMY_MOVE_SYNC
{
    public int InstanceId;
    public float PosX, PosY, PosZ;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_ENEMY_ATTACK_ANIM
{
    public int InstanceId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_ONESHOT_ANIM_SYNC
{
    public int UserDBID;
    public int AnimCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_INTERACT_SYNC
{
    public int UserDBID;
    public byte IsStart;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_DEAD_SYNC
{
    public int UserDBID;
    public byte IsStart;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_COMBAT_STATE_SYNC
{
    public int UserDBID;
    public byte IsCombat;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_MAP_CHANGE_REQ
{
    public int UserDBID;
    public int TargetMapId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_MAP_CHANGE_ACK
{
    public int MapId;
    public float PosX;
    public float PosY;
    public float PosZ;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_C2S_ENEMY_DEAD_REQ
{
    public int InstanceId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_S2C_ENEMY_DEAD_ACK
{
    public int InstanceId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PKT_QUEST_DATA
{
    public int QuestId;
    public int IsCompleted;
    public int RewardClaimed;
    public int ConditionCount;
    public fixed int CurrentCounts[8];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PKT_QUEST_RESET
{
    public int UserDBID;
}
