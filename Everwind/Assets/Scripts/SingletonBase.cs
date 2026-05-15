using UnityEngine;

public class SingletonBase<T> : SingletonBasest where T : MonoBehaviour
{
    private static T _instance;

    protected static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (IsPersistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            SingletonManager.Instance.Register(this);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            SingletonManager.Instance?.Unregister(this);
            _instance = null;
        }
    }
    public override void Init() { }
}

