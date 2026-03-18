using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    public int InstanceNum;
    [SerializeField] protected float Hp = 100f; // 로컬 연산용이 아닌 서버 동기화용 HP

    protected Animator Animator;
    protected AnimatorOverrideController OverrideController;

    public AnimationClip Damaged1;
    public AnimationClip Damaged2;
    public AnimationClip Damaged3;

    protected bool IsDead = false;
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

        Debug.Log($"[{gameObject.name}] 서버로 공격 판정 요청 (계산된 데미지 : {damage})");
    }

    public virtual void SyncDamage(float serverHp, float damageAmount)
    {
        if (IsDead) return;

        Hp = serverHp;

        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Damaged", this.transform.position);
        UIEvents.RaiseEnemyDamageTextRequested(transform.position, (int)damageAmount);

        Debug.Log($"[{gameObject.name}] 서버 동기화 완료 - 남은 HP : {Hp}");
    }

    public virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 콜라이더 끄기 (더 이상 타겟팅되거나 길을 막지 않음)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // CharacterController 끄기
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        OnEnemyDied?.Invoke(this);
        SingletonManager.Instance.GetSingleton<EnemySpawner>().RemoveEnemy(this.InstanceNum);
        

        Destroy(gameObject);
        Debug.Log($"[{gameObject.name}] 사망 (서버 판정)");
    }

    public float GetHp()
    {
        return Hp;
    }


}
