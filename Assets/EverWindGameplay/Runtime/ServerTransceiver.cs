using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class ServerTransceiver : MonoBehaviour
{
    public int EntityId { get; private set; }
    public EntityKind Kind { get; private set; }
    public float Hp { get; private set; }
    public float MaxHp { get; private set; }
    public float Radius { get; private set; }
    public int Stock { get; private set; }
    public ulong ActionId { get; private set; }
    public PlayerStateContext State { get; } = new PlayerStateContext();
    public NetworkClient Transport { get; private set; }
    public ServerTransceiverManager Registry { get; private set; }
    public bool IsLocal =>
        Kind == EntityKind.Player && Transport != null && Transport.LocalEntityId == EntityId;
    IServerMessageHandler[] handlers = Array.Empty<IServerMessageHandler>();
    public void Bind(ServerTransceiverManager registry, ServerMessage spawn)
    {
        Registry = registry;
        Transport = registry.Transport;
        EntityId = spawn.Entity;
        Kind = (EntityKind)spawn.Code;
        Radius = spawn.Radius;
        Hp = spawn.Hp;
        MaxHp = spawn.MaxHp;
        Stock = (int)spawn.Value;
        var list = new List<IServerMessageHandler>();
        foreach (var c in GetComponents<MonoBehaviour>())
            if (c is IServerMessageHandler h)
                list.Add(h);
        handlers = list.ToArray();
        State.Apply(spawn.Mode);
        transform.position = spawn.Position;
        Registry.Register(this);
    }
    public ulong Send(Request request, Action<ServerMessage> callback = null)
    {
        if (!IsLocal)
            throw new InvalidOperationException("Remote entities cannot submit player requests");
        return Transport.Send(request, callback);
    }
    public void Receive(ServerMessage m)
    {
        if (m.Kind == EventKind.State || m.Kind == EventKind.Hit)
        {
            Hp = m.Hp;
            MaxHp = m.MaxHp;
        }
        if (Kind == EntityKind.Resource && m.Kind == EventKind.State)
            Stock = (int)m.Value;
        if (m.Kind == EventKind.State || m.Kind == EventKind.Hit || m.Kind == EventKind.ActionStarted ||
            m.Kind == EventKind.ActionEnded || m.Kind == EventKind.ActionCancelled)
            State.Apply(m.Mode);
        if (m.Kind == EventKind.ActionStarted)
            ActionId = m.ActionId;
        if ((m.Kind == EventKind.ActionEnded || m.Kind == EventKind.ActionCancelled) &&
            ActionId == m.ActionId)
            ActionId = 0;
        foreach (var handler in handlers)
            handler.Handle(m);
    }
    void OnDestroy()
    {
        if (Registry != null)
            Registry.Unregister(this);
    }
}
}
