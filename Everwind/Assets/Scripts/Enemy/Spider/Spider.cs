using UnityEngine;

public class Spider : Enemy
{
    public AnimationClip SpiderDamaged1;
    public AnimationClip SpiderDamaged2;
    public AnimationClip SpiderDamaged3;

    protected override void Start()
    {
        base.Start();

        Damaged1 = SpiderDamaged1;
        Damaged2 = SpiderDamaged2;
        Damaged3 = SpiderDamaged3;
    }

    public override Vector3 GetRotationOffsetEuler()
    {
        return new Vector3(0f, 180f, 0f);
    }
}
