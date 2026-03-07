using System;
using System.Collections;
using UnityEngine;

public abstract class FieldItem : MonoBehaviour, IObtainable
{
    protected string Id;
    public int InstancedId;
    protected float ObtainTime;
    protected bool IsRespawnsible;
    public event Action EvObtained;
    protected IEnumerator ObtainingCoroutine;

    protected abstract void Start();

    protected abstract IEnumerator Obtaining();

    public virtual void StartRooting()
    {
        ObtainingCoroutine = Obtaining();
        StartCoroutine(ObtainingCoroutine);
    }

    public virtual bool IsNullCoroutine()
    {
        return ObtainingCoroutine == null;
    }

    public virtual void StopRooting()
    {
        if (ObtainingCoroutine != null)
        {
            StopCoroutine(ObtainingCoroutine);
            ObtainingCoroutine = null;
        }
    }

    protected void EndRooting()
    {
        EvObtained?.Invoke();
        ObtainingCoroutine = null;
    }

    public virtual void Initialize()
    { }
}