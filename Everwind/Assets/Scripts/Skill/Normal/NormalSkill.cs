
using UnityEngine;

public class NormalSkill : Skill
{   

    public override void Cast(Transform attacker)
    {
        Init();
        IsLocked = false;
        GameObject target = GetTarget(attacker);

        DealDamage(attacker, target, Dmg);
        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Skill_normal", attacker.position);
    }
   
    protected override void Init()
    {   

        SkillAnimationName = "NormalSkill";
        CoolTime = 0.3f;
      
        IsLocked = false;
        Dmg = 10.0f;
    }


    protected override void Update()
    {
        
    }
}



