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

        if (transform.childCount > 0)
        {
            Transform visualModel = transform.GetChild(0);
            visualModel.localRotation = Quaternion.Euler(0, 180f, 0);
        }
    }
}