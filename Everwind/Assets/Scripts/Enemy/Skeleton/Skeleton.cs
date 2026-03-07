using UnityEngine;

public class Skeleton : Enemy
{
    public AnimationClip SkeletonDamaged1;
    public AnimationClip SkeletonDamaged2;
    public AnimationClip SkeletonDamaged3;

    protected override void Start()
    {
        base.Start();

        Damaged1 = SkeletonDamaged1;
        Damaged2 = SkeletonDamaged2;
        Damaged3 = SkeletonDamaged3;
    }
}