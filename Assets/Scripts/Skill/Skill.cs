using UnityEngine;

public abstract class Skill : MonoBehaviour
{
    public string SkillAnimationName;
    public float CoolTime;
    public bool IsCasted;
    protected float Dmg;
    protected bool IsLocked = false;

    public abstract void Cast(Transform initTransform);

    protected virtual void DealDamage(Transform attacker, GameObject target, float dmg)
    {
        if (target == null) return;
        IDamageable dmgComp = target.GetComponent<IDamageable>();
        float result = attacker.GetComponent<PlayerStatManager>().CalculateFinalDamage(dmg);
        if (dmgComp != null)
            dmgComp.TakeDamage(result, attacker);
    }

    public virtual GameObject GetTarget(Transform attacker)
    {
        CombatManager combatManager = attacker.GetComponent<CombatManager>();
        if (combatManager != null && combatManager.TargetEnemy != null)
        {
            return combatManager.TargetEnemy;
        }
        else
            return null;
    }

    protected virtual void Init()
    {
    }

    protected virtual void Update()
    {
    }
}
