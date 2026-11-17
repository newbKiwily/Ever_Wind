using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class ServerTransmitterManager : MonoBehaviour
{
    private readonly Dictionary<int, ServerTransmitter> entities = new Dictionary<int, ServerTransmitter>();
    private readonly Dictionary<int, int> inventory = new Dictionary<int, int>();

    public NetworkClient Transport;
    public bool DebugGeometry;
    public GameplayCatalog Catalog => ContainerInMap.Current.Catalog;
    public int CurrentMap { get; private set; }
    public Vector3 MapCenter => ContainerInMap.Current.MapCenter;
    public float MapRadius => ContainerInMap.Current.MapRadius;
    public GameObject TargetIndicatorVisual => ContainerInMap.Current.GetAsset<GameObject>(ContainerInMap.Current.TargetIndicatorVisual);
    public IReadOnlyDictionary<int, int> Inventory => inventory;
    public IEnumerable<ServerTransmitter> Entities => entities.Values;
    public string ZoneStatus { get; private set; } = "Outside event zone";
    public PlayerServerTransmitter LocalActor => Transport != null && entities.TryGetValue(Transport.LocalEntityId, out ServerTransmitter actor) ? actor as PlayerServerTransmitter : null;

    private void OnEnable()
    {
        Transport = NetworkClient.Current;

        if (Transport != null)
        {
            Transport.Message += Receive;
        }
    }

    private void OnDisable()
    {
        if (Transport != null)
        {
            Transport.Message -= Receive;
        }

        Clear();
    }

    public float SkillReach(int id)
    {
        return Catalog != null ? Catalog.Reach(id) : 0;
    }

    public void Register(ServerTransmitter actor)
    {
        entities[actor.EntityId] = actor;
    }

    public void Unregister(ServerTransmitter actor)
    {
        if (entities.TryGetValue(actor.EntityId, out ServerTransmitter current) && current == actor)
        {
            entities.Remove(actor.EntityId);
        }
    }

    public bool TryGet(int id, out ServerTransmitter actor)
    {
        return entities.TryGetValue(id, out actor) && actor != null;
    }

    public void Clear()
    {
        entities.Clear();
        inventory.Clear();
        ZoneStatus = "Outside event zone";
    }

    private void Receive(ServerMessage message)
    {
        switch (message.Kind)
        {
        case EventKind.Welcome:
            Clear();
            CurrentMap = (int)message.Value;
            break;

        case EventKind.Spawn:
            if (ContainerInMap.Current.Spawns.ContainsKey(message.Entity))
            {
                ContainerInMap.Current.Loader.ReceiveSpawn(message);
            }
            break;

        case EventKind.Despawn:
            ContainerInMap.Current.Loader.Despawn(message.Entity);
            break;

        case EventKind.Inventory:
            inventory[message.Definition] = (int)message.Value;
            break;

        case EventKind.Zone:
            if (message.Entity == Transport.LocalEntityId)
            {
                ZoneStatus = "Zone " + message.Target + ((message.Flags & 1) != 0 ? ": Entered" : ": Exited");
            }
            break;
        }

        if (entities.TryGetValue(message.Entity, out ServerTransmitter actor))
        {
            actor.Receive(message);
        }
        else
        {
            ContainerInMap.Current.Loader.BufferEntityEvent(message);
        }
    }
}
}
