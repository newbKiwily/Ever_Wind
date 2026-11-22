using UnityEngine;

public abstract class SingletonBasest : MonoBehaviour
{
    public int Priority;
    public virtual bool IsPersistent => false;
    public abstract void Init();
}

