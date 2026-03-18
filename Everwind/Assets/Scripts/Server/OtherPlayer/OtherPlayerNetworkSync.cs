using UnityEngine;

[RequireComponent(typeof(AnimationContexter))]
public class OtherPlayerNetworkSync : MonoBehaviour
{
    private AnimationContexter _animContexter;
    public bool IsNormalState = true;

    public bool IsDead = false;
    public bool IsInteracting = false;

    private void Awake()
    {
        _animContexter = GetComponent<AnimationContexter>();
    }

    public void OnReceiveMoveSync(Vector3 pos, float speed)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        transform.position = pos;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);

        // 사망이거나 상호작용 중일 때는 걷기/대기 애니메이션으로 덮어씌우지 않음
        if (IsDead || IsInteracting) return;

        if (speed > 0.03f)
        {
            _animContexter.PlayMove(IsNormalState);
        }
        else
        {
            _animContexter.PlayIdle(IsNormalState);
        }
    }

    public void OnReceiveOneshot(int animCode)
    {
        if (IsDead) return; // 죽어있을 때는 원샷 모션 무시

        OneshotAnimKey key = (OneshotAnimKey)animCode;
        switch (key)
        {
            case OneshotAnimKey.Attack1:
                _animContexter.PlayAttack(1);
                PlayAttackEffect(1);
                break;
            case OneshotAnimKey.Attack2:
                _animContexter.PlayAttack(2);
                PlayAttackEffect(2);
                break;
            case OneshotAnimKey.Attack3:
                _animContexter.PlayAttack(3);
                PlayAttackEffect(3);
                break;
            case OneshotAnimKey.Attack4:
                _animContexter.PlayAttack(4);
                PlayAttackEffect(4);
                break;
            case OneshotAnimKey.Attack5:
                _animContexter.PlayAttack(5);
                PlayAttackEffect(5);
                break;
            case OneshotAnimKey.Damaged1:
                _animContexter.PlayDamaged(1);
                PlayDamagedEffect();
                break;
            case OneshotAnimKey.Damaged2:
                _animContexter.PlayDamaged(2);
                PlayDamagedEffect();
                break;
            case OneshotAnimKey.Damaged3:
                _animContexter.PlayDamaged(3);
                PlayDamagedEffect();
                break;
            case OneshotAnimKey.Success:
                _animContexter.PlayOneshot(OneshotAni.Success);
                break;
        }
    }

    public void OnReceiveInteract(bool isStart)
    {
        if (IsDead) return;

        IsInteracting = isStart;
        if (IsInteracting)
        {
            _animContexter.PlayInteract();
        }
        else
        {
            _animContexter.ExitInteract();
        }
    }

    public void OnReceiveDead(bool isStart)
    {
        IsDead = isStart;
        if (IsDead)
        {
            _animContexter.PlayDead();
        }
        else
        {
            _animContexter.ExitDead();
        }
    }

    public void SetIsNormal(bool isCombat)
    {
        if (isCombat)
        {
            IsNormalState = false;
            return;
        }

        IsNormalState = true;
    }

    private void PlayAttackEffect(int skillIndex)
    {
        string effectKey = null;

        switch (skillIndex)
        {
            case 1:
                effectKey = "Skill_normal";
                break;
            case 2:
                effectKey = "Skill_smash";
                break;
            case 3:
                effectKey = "Skill_windmill";
                break;
        }

        if (string.IsNullOrEmpty(effectKey))
            return;

        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect(effectKey, transform.position);
    }

    private void PlayDamagedEffect()
    {
        SingletonManager.Instance.GetSingleton<EffectManager>().PlayEffect("Damaged", transform.position);
    }
}
