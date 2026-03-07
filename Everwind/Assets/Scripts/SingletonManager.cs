using System;
using System.Collections.Generic;
using System.Linq;
public class SingletonManager : SingletonBase<SingletonManager>
{

    private List<SingletonBasest> _ingameSingletonsForInit = new List<SingletonBasest>();
    private List<SingletonBasest> _serverSingletonsForInit = new List<SingletonBasest>();
    private Dictionary<Type, SingletonBasest> _singletons = new Dictionary<Type, SingletonBasest>();

    public new static SingletonManager Instance => SingletonBase<SingletonManager>.Instance;

    protected override void Awake()
    {
        Priority = -100;
        base.Awake();
    }

    private void Start()
    {
        this.InitializeServer();
    }

    public void Register(SingletonBasest manager)
    {
        Type type = manager.GetType();
        if (!_singletons.ContainsKey(type))
        {
            if (manager.Priority < 0)
                _serverSingletonsForInit.Add(manager);
            else
                _ingameSingletonsForInit.Add(manager);

            _singletons.Add(type, manager);
        }
    }

    public T GetSingleton<T>() where T : SingletonBasest
    {
        Type type = typeof(T);
        if (_singletons.TryGetValue(type, out var manager))
        {
            return manager as T;
        }

        return null;
    }

    private void InitializeServer()
    {
        var sortedList = _serverSingletonsForInit.OrderBy(singleton => singleton.Priority).ToList();

        foreach (var singleton in sortedList)
        {
            singleton.Init();
        }
    }

    public void InitializeIngame()
    {
        var sortedList = _ingameSingletonsForInit.OrderBy(singleton => singleton.Priority).ToList();

        foreach (var singleton in sortedList)
        {
            singleton.Init();
        }
    }
}