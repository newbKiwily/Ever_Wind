using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class ServerTransmitterManager : MonoBehaviour
{
    private readonly Dictionary<int, ServerTransmitter> entities = new Dictionary<int, ServerTransmitter>();
    private readonly Dictionary<int, int> inventory = new Dictionary<int, int>();

    public NetworkClient Transport;
    public PlayerProgression Progression { get; } = new PlayerProgression();
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

    private void OnDisable()
    {
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
        Progression.Clear();
        ZoneStatus = "Outside event zone";
    }

    public void Receive(ServerMessage message)
    {
        if (message.Kind == EventKind.Growth || message.Kind == EventKind.SkillRank)
        {
            if (message.Entity == Transport.LocalEntityId)
            {
                Progression.Apply(message);
            }

            // 성장 전용 필드를 월드 좌표나 엔티티 상태로 전달하지 않는다.
            return;
        }

        if (message.Entity == Transport.LocalEntityId)
        {
            Progression.Apply(message);
        }

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
