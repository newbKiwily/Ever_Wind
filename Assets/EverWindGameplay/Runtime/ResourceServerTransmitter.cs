using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class ResourceServerTransmitter : ServerTransmitter
{
    private Renderer[] renderers;
    private Collider[] colliders;

    private double nextRefillTry;

    private uint refillGeneration;

    private bool depleted;

    public int Stock { get; private set; }
    protected override void InitializeEntity(ServerMessage spawn)
    {
        renderers = GetComponentsInChildren<Renderer>();

        colliders = GetComponents<Collider>();

        Stock = (int)spawn.Value;

        ApplyVisibility();
    }

    protected override bool CanSend(Request request)
    {
        return Transport != null && Transport.Ready && request.Kind == RequestKind.ResourceRefill && request.Target == EntityId;
    }

    public override void Receive(ServerMessage message)
    {
        if (message.Kind == EventKind.State)
        {
            Stock = (int)message.Value;

            ApplyVisibility();

            depleted = Stock <= 0;

            refillGeneration = message.Sequence;

            nextRefillTry = Transport.EstimatedServerTime + message.Duration;
        }

        base.Receive(message);
    }

    private void ApplyVisibility()
    {
        foreach (var renderer in renderers)
        {
            renderer.enabled = Stock > 0;

            renderer.SetPropertyBlock(null);
        }

        foreach (var collider in colliders)
        {
            collider.enabled = Stock > 0;
        }
    }

    private void Update()
    {
        if (!depleted || Transport == null || !Transport.Ready || Transport.EstimatedServerTime < nextRefillTry)
        {
            return;
        }

        nextRefillTry = Transport.EstimatedServerTime + .5;
        // 로컬 타이머는 외형을 복구하고 서버가 잔량과 리필 세대를 확정한다.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = true;
        }

        Send(new Request { Kind = RequestKind.ResourceRefill, Target = EntityId, Sequence = refillGeneration });
    }
}
}
