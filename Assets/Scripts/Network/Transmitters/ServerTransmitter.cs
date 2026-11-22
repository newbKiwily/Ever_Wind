using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public class ServerTransmitter : MonoBehaviour
{
    IServerMessageHandler[] handlers = Array.Empty<IServerMessageHandler>();

    public int EntityId { get; private set; }
    public EntityKind Kind { get; private set; }
    public float Radius { get; private set; }
    public NetworkClient Transport { get; private set; }
    public ServerTransmitterManager Registry { get; private set; }
    public virtual bool IsLocal => false;
    public void Bind(ServerTransmitterManager registry, ServerMessage spawn)
    {
        Registry = registry;
        Transport = registry.Transport;
        EntityId = spawn.Entity;
        Kind = (EntityKind)spawn.Code;
        Radius = spawn.Radius;
        var list = new List<IServerMessageHandler>();
        foreach (var c in GetComponents<MonoBehaviour>())
            if (c is IServerMessageHandler h)
            {
                list.Add(h);
            }
        handlers = list.ToArray();
        InitializeEntity(spawn);
        transform.position = spawn.Position;
        Registry.Register(this);

        foreach (var handler in handlers)
        {
            handler.Handle(spawn);
        }
    }

    public ulong Send(Request request, Action<ServerMessage> callback = null)
    {
        if (!CanSend(request))
        {
            throw new InvalidOperationException("Remote entities cannot submit player requests");
        }
        return Transport.Send(request, callback);
    }

    public virtual void Receive(ServerMessage m)
    {
        foreach (var handler in handlers)
            handler.Handle(m);
    }

    protected virtual void InitializeEntity(ServerMessage spawn)
    {
    }

    protected virtual bool CanSend(Request request)
    {
        return false;
    }

    protected virtual void OnDestroy()
    {
        if (Registry != null)
        {
            Registry.Unregister(this);
        }
    }
}
}
