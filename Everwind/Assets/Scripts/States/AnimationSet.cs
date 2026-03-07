using UnityEngine;

public enum OneshotAni
{
    Success
}

[CreateAssetMenu]
public class AnimationSet : ScriptableObject
{

    public AnimationClip KeyIdle;
    public AnimationClip KeyMove;
    public AnimationClip KeyOneshot;
    public AnimationClip KeyDamaged;
    public AnimationClip KeyAttack;

    public AnimationClip IdleNormal;
    public AnimationClip IdleCombat;

    public AnimationClip MoveNormal;
    public AnimationClip MoveCombat;

    public AnimationClip OS_Success;
    public AnimationClip Damaged1, Damaged2, Damaged3;

    public AnimationClip Attack1, Attack2, Attack3, Attack4, Attack5;

    public AnimationClip Dead;
}