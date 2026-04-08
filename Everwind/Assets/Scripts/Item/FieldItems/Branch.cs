using System.Collections;
using UnityEngine;

public class Branch : FieldItem
{
    protected override void Start()
    {
        ObtainTime = 5.0f;
        Id = "Branch";
        IsRespawnsible = true;
    }

    protected override IEnumerator Obtaining()
    {
        while (ObtainTime > 0)
        {
            ObtainTime -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("채집이 완료되었습니다!");

        var mediator = SingletonManager.Instance.GetSingleton<ItemMediator>();
        mediator.Mediation(Id);

        if (IsRespawnsible)
        {
            mediator.ItemRespawn(this.gameObject, 5.0f);
        }
        else
        {
            Initialize();
        }

        EndRooting();
    }

    public override void Initialize()
    {
        ObtainTime = 5.0f;
    }
}
