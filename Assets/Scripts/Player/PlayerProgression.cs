using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
// 서버가 확정한 개인 성장 정보만 보관한다. 포인트 소비와 경험치 계산은 서버에서 수행한다.
public sealed class PlayerProgression
{
    public sealed class SkillProgress
    {
        public int Rank { get; internal set; }
        public float DamageMultiplier { get; internal set; }
        public float Cooldown { get; internal set; }
        public double CooldownEndsAt { get; internal set; }
    }

    private readonly Dictionary<int, SkillProgress> skills = new Dictionary<int, SkillProgress>();

    public bool IsLoaded { get; private set; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public int RequiredExperience { get; private set; }
    public int SkillPoints { get; private set; }
    public int StatPoints { get; private set; }
    public int AttackRank { get; private set; }
    public int DefenceRank { get; private set; }
    public int MoveSpeedRank { get; private set; }
    public int AttackPower { get; private set; }
    public int DefencePower { get; private set; }
    public float MoveSpeed { get; private set; }
    public IReadOnlyDictionary<int, SkillProgress> Skills => skills;

    public bool CanUseSkill(int id) => IsLoaded && skills.TryGetValue(id, out SkillProgress skill) && skill.Rank > 0;

    internal void Apply(ServerMessage message)
    {
        switch (message.Kind)
        {
        case EventKind.Growth:
            // Growth에서 Position은 스탯 랭크, Radius는 남은 스탯 포인트이다.
            Level = message.Code;
            Experience = message.Target;
            RequiredExperience = message.Definition;
            SkillPoints = Mathf.RoundToInt(message.Value);
            StatPoints = Mathf.RoundToInt(message.Radius);
            AttackRank = Mathf.RoundToInt(message.Position.x);
            DefenceRank = Mathf.RoundToInt(message.Position.y);
            MoveSpeedRank = Mathf.RoundToInt(message.Position.z);
            IsLoaded = true;
            break;

        case EventKind.SkillRank:
            skills[message.Definition] = new SkillProgress
            {
                Rank = message.Code,
                DamageMultiplier = message.Value,
                Cooldown = message.Duration,
                CooldownEndsAt = message.ServerTime + message.HitTime
            };
            break;

        case EventKind.Stats:
            AttackPower = message.Definition;
            DefencePower = message.Target;
            MoveSpeed = message.Value;
            break;

        case EventKind.ActionStarted:
            if ((ActionKind)message.Code == ActionKind.Attack && skills.TryGetValue(message.Definition, out SkillProgress used))
            {
                used.CooldownEndsAt = message.ServerTime + used.Cooldown;
            }
            break;
        }
    }

    internal void Clear()
    {
        IsLoaded = false;
        Level = Experience = RequiredExperience = SkillPoints = StatPoints = 0;
        AttackRank = DefenceRank = MoveSpeedRank = AttackPower = DefencePower = 0;
        MoveSpeed = 0;
        skills.Clear();
    }
}
}
