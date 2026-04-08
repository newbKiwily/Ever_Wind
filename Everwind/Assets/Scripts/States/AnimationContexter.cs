using UnityEngine;

public class AnimationContexter : MonoBehaviour
{
    private Animator _animator;
    private AnimatorOverrideController _overrideController;

    [SerializeField] private AnimationSet _animationSet;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
        }
    }

    
    public void PlayIdle(bool isNormal)
    {
        AnimationClip key = _animationSet.KeyIdle;


        if (isNormal)
        {
            _overrideController[key] = _animationSet.IdleNormal;
        }
        else
        {
            _overrideController[key] = _animationSet.IdleCombat;
        }

        _animator.SetBool("isMove", false);
    }

    public void PlayMove(bool isNormal)
    {
        AnimationClip key = _animationSet.KeyMove;

        if (isNormal)
        {
            _overrideController[key] = _animationSet.MoveNormal;
        }
        else
        {
            _overrideController[key] = _animationSet.MoveCombat;
        }


        _animator.SetBool("isMove", true);
    }
    public int PlayDamaged(int count)
    {
        AnimationClip key = _animationSet.KeyDamaged;
        switch (count)
        {
            case 1:
                {
                    _overrideController[key] = _animationSet.Damaged1;
                    _animator.SetTrigger("toDamaged");
                    return 11;
                }
            case 2:
                {
                    _overrideController[key] = _animationSet.Damaged2;
                    _animator.SetTrigger("toDamaged");
                    return 12;
                }
            case 3:
                {
                    _overrideController[key] = _animationSet.Damaged3;
                    _animator.SetTrigger("toDamaged");
                    return 13;
                }
            default:
                return 11;
        }

    }

    public void PlayAttack(int idx)
    {
        AnimationClip key = _animationSet.KeyAttack;
        switch(idx)
        {
            case 1:
                {
                    _animator.SetFloat("SkillSpeed", 2.0f);
                    _overrideController[key] = _animationSet.Attack1;
                    break;
                }
            case 2:
                {
                    _animator.SetFloat("SkillSpeed", 1.4f);
                    _overrideController[key] = _animationSet.Attack2;
                    break;
                }
            case 3:
                {
                    _animator.SetFloat("SkillSpeed", 1.5f);
                    _overrideController[key] = _animationSet.Attack3;
                    break;
                }
            case 4:
                {
                    _animator.SetFloat("SkillSpeed", 2.0f);
                    _overrideController[key] = _animationSet.Attack4;
                    break;
                }
            case 5:
                {
                    _animator.SetFloat("SkillSpeed", 2.0f);
                    _overrideController[key] = _animationSet.Attack5;
                    break;
                }
        }
        _animator.SetTrigger("toAttack");

    }
    public void PlayInteract()
    {
        _animator.SetBool("isInteract", true);
        _animator.Play("Interact", 0, 0.0f);
    }
    public void ExitInteract()
    {
        _animator.SetBool("isInteract", false);
    }
    public void PlayDead()
    {
        _animator.SetBool("isDie", true);
        
    }
    public void ExitDead()
    {
        _animator.SetBool("isDie", false);
    }

    public void PlayOneshot(OneshotAni ani)
    {
        AnimationClip key = _animationSet.KeyOneshot;
        switch (ani)
        {
            case OneshotAni.Success:
                {
                    _overrideController[key] = _animationSet.OS_Success;
                    break;
                }
            default:
                return;
        }
        _animator.SetTrigger("toOneshot");

    }
}

