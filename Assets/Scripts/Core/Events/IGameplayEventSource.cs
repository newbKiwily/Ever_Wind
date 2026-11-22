using System;

namespace EverWind.Gameplay
{
public enum GameplayFeedback
{
    Hit,
    Success
}

public struct HealthChanged
{
    public int Entity;
    public float Hp, MaxHp;
}

public struct SkillCooldownChanged
{
    public int Skill;
    public double EndsAt;
    public float Duration;
}

public interface IGameplayEventSource
{
    event Action<HealthChanged> HealthChanged;
    event Action<SkillCooldownChanged> SkillCooldownChanged;
    event Action<GameplayFeedback> Feedback;
    HealthChanged CurrentHealth { get; }
    SkillCooldownChanged GetSkillCooldown(int skill);
}
}
