using UnityEngine;

public class SmashSkill : Skill
{
    public override void Cast(Transform attacker)
    {
        Init();

        IsLocked = false;
        GameObject target = GetTarget(attacker);

        DealDamage(attacker, target, Dmg);
        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Skill_smash", attacker.position);
    }

    protected override void Init()
    {
        Dmg = 15.0f;
        CoolTime = 1.5f;
        SkillAnimationName = "SmashSkill";
        
    }


    protected override void Update()
    {

    }
}

