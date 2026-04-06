using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    public int InstanceNum;
    [SerializeField] protected float Hp = 100f;

    protected Animator Animator;
    protected AnimatorOverrideController OverrideController;

    public AnimationClip Damaged1;
    public AnimationClip Damaged2;
    public AnimationClip Damaged3;

    protected bool IsDead = false;
    protected bool DeadRequestSent = false;
    public event System.Action<Enemy> OnEnemyDied;

    public void Init(int instanceNum)
    {
        this.InstanceNum = instanceNum;
    }

    protected virtual void Start()
    {
        Animator = GetComponent<Animator>();

        if (Animator != null && Animator.runtimeAnimatorController != null)
        {
            OverrideController = new AnimatorOverrideController(Animator.runtimeAnimatorController);
            Animator.runtimeAnimatorController = OverrideController;
        }
    }

    public void SwitchDamagedMotion(int damagedCount)
    {
        if (OverrideController == null) return;

        switch (damagedCount)
        {
            case 1:
                if (Damaged1 != null) OverrideController["damaged1"] = Damaged1;
                break;
            case 2:
                if (Damaged2 != null) OverrideController["damaged1"] = Damaged2;
                break;
            case 3:
                if (Damaged3 != null) OverrideController["damaged1"] = Damaged3;
                break;
        }
    }

    public virtual void TakeDamage(float damage, Transform attacker)
    {
        if (IsDead) return;

        byte[] attackPkt = PacketMethod.BuildAttackReq(this.InstanceNum, damage);
        SingletonManager.Instance.GetSingleton<NetworkClient>().Send(attackPkt);
    }

    public virtual void SyncDamage(float serverHp, float damageAmount)
    {
        if (IsDead) return;

        Hp = serverHp;

        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Damaged", this.transform.position);
        UIEvents.RaiseDamageTextRequested(transform.position, Mathf.RoundToInt(damageAmount));
    }

    public bool ShouldRequestDeath()
    {
        if (IsDead || DeadRequestSent || Hp > 0f)
            return false;

        DeadRequestSent = true;
        return true;
    }

    public virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        OnEnemyDied?.Invoke(this);
        TutorialEvents.RaiseCombatEnemyKilled();
        SingletonManager.Instance.GetSingleton<EnemySpawner>().RemoveEnemy(this.InstanceNum);

        Destroy(gameObject);
    }

    public float GetHp()
    {
        return Hp;
    }

    public virtual Vector3 GetRotationOffsetEuler()
    {
        return Vector3.zero;
    }
}
