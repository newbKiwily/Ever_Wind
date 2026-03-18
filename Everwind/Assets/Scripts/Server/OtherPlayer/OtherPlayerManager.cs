using System.Collections.Generic;
using UnityEngine;

public class OtherPlayerManager : SingletonBase<OtherPlayerManager>
{
    public override bool IsPersistent => false;

    private Dictionary<int, OtherPlayer> _instancedPlayers = new Dictionary<int, OtherPlayer>();

    protected override void Awake()
    {
        Priority = 90;
        base.Awake();
    }

    public void RegisterPlayer(int userDbId, OtherPlayer target)
    {
        if (_instancedPlayers.ContainsKey(userDbId))
            return;

        _instancedPlayers.Add(userDbId, target);
        target.Init(userDbId);
    }

    public void RemovePlayer(int userDbId)
    {
        if (_instancedPlayers.TryGetValue(userDbId, out OtherPlayer target))
        {
            if (target != null && target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
            _instancedPlayers.Remove(userDbId);
        }
    }

    public OtherPlayer FindPlayer(int userDbId)
    {
        if (_instancedPlayers.TryGetValue(userDbId, out OtherPlayer target))
        {
            return target;
        }

        return null;
    }
}
