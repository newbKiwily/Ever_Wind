using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : SingletonBase<EnemySpawner>
{
    public override bool IsPersistent => false;

    private Dictionary<int, Enemy> _instancedEnemies = new Dictionary<int, Enemy>();

    protected override void Awake()
    {
        Priority = 80;
        base.Awake();
    }

    public void RegisterEnemy(int id, Enemy target)
    {
        if (_instancedEnemies.ContainsKey(id))
            return;

        _instancedEnemies.Add(id, target);
    }

    public Enemy FindEnemy(int instanceId)
    {
        if (_instancedEnemies.TryGetValue(instanceId, out Enemy enemy))
        {
            return enemy;
        }
        return null;
    }

    public void RemoveEnemy(int instanceId)
    {
        _instancedEnemies.Remove(instanceId);
    }

    public void FlushEnemy()
    {
        foreach(var enemy  in _instancedEnemies.Values)
            Destroy(enemy);
        _instancedEnemies.Clear();
    }
}
