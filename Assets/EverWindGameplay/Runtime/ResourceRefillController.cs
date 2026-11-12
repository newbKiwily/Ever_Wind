using UnityEngine;
namespace EverWind.Gameplay
{
public sealed class ResourceRefillController : MonoBehaviour, IServerMessageHandler
{
    public ServerTransceiver Actor;
    double at, nextTry;
    uint generation;
    bool depleted;
    public void Handle(ServerMessage m)
    {
        if (m.Kind != EventKind.State)
            return;
        depleted = m.Value <= 0;
        generation = m.Sequence;
        at = Actor.Transport.EstimatedServerTime + m.Duration;
        nextTry = at;
    }
    void Update()
    {
        if (!depleted || Actor == null || !Actor.Transport.Ready ||
            Actor.Transport.EstimatedServerTime < nextTry)
            return;
        nextTry = Actor.Transport.EstimatedServerTime + .5;
        // Local timer restores the presentation; server still validates elapsed time and the refill
        // generation.
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = true;
        Actor.Registry.LocalActor?.Send(new Request { Kind = RequestKind.ResourceRefill,
                                                      Target = Actor.EntityId, Sequence = generation });
    }
}
}
