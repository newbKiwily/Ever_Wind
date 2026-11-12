using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class MovementMotor : MonoBehaviour, IServerMessageHandler
{
    public float Radius = .45f;
    public float MoveSpeed = 5;
    public LayerMask BlockingMask;
    public ServerTransceiver Actor;
    uint sequence, revision, confirmed;
    float sendTimer;
    bool dirty;
    readonly Dictionary<uint, Vector3> predicted = new Dictionary<uint, Vector3>();
    public bool Forced { get; set; }
    public void Step(Vector3 delta, bool request = true)
    {
        delta.y = 0;
        float distance = delta.magnitude;
        if (distance < .00001f)
            return;
        Vector3 desiredDirection = delta;
        // Same conservative square footprint as the server's radius-expanded boxes.
        var origin = transform.position + Vector3.up;
        float allowedDistance = distance;
        // Keep entity colliders for targeting/interactions, but do not use them to block movement.
        // Inspect every hit so a passable resource cannot hide a wall behind it.
        foreach (var hit in Physics.BoxCastAll(origin, new Vector3(Radius, .9f, Radius), delta / distance,
                                               Quaternion.identity, distance + .01f, BlockingMask,
                                               QueryTriggerInteraction.Ignore))
        {
            var entity = hit.collider.GetComponentInParent<ServerTransceiver>();
            if (entity != null && (entity.Kind == EntityKind.Player || entity.Kind == EntityKind.Monster ||
                                   entity.Kind == EntityKind.Resource))
                continue;
            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0, hit.distance - .01f));
        }
        delta = delta.normalized * allowedDistance;
        var next = transform.position + delta;
        var center = Actor.Registry.MapCenter;
        float limit = Actor.Registry.MapRadius - Radius;
        if (new Vector2(next.x - center.x, next.z - center.z).magnitude > limit)
        {
            var xz = new Vector2(next.x - center.x, next.z - center.z).normalized *limit;
            next.x = xz.x + center.x;
            next.z = xz.y + center.z;
        }
        if (Actor.Registry.CurrentLevel != null)
            next.y = Actor.Registry.CurrentLevel.Height(next.x, next.z);
        transform.position = next;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDirection),
                                              Time.deltaTime * 10f);
        if (request)
            dirty = true;
    }
    void Update()
    {
        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < .05f || !dirty || Forced || Actor == null || !Actor.Transport.Ready)
            return;
        FlushPending();
    }
    // The final predicted move must precede an action request on the same TCP stream.
    public void FlushPending()
    {
        if (!dirty || Forced || Actor == null || !Actor.Transport.Ready)
            return;
        sendTimer = 0;
        dirty = false;
        uint seq = ++sequence;
        predicted[seq] = transform.position;
        if (predicted.Count > 128)
        {
            predicted.Clear();
            Actor.Transport.CloseConnection();
            return;
        }
        Actor.Send(new Request { Kind = RequestKind.Move, Position = transform.position, Sequence = seq,
                                 Revision = revision });
    }
    public void Handle(ServerMessage m)
    {
        if (m.Kind == EventKind.Hit && (m.Flags & 1) != 0)
        {
            revision = m.Revision;
            predicted.Clear();
            dirty = false;
            return;
        }
        if (m.Kind == EventKind.State)
        {
            if (m.Revision > revision || m.Mode == PlayerMode.Dead || Forced)
            {
                revision = m.Revision;
                sequence = MathfMax(sequence, m.Sequence);
                predicted.Clear();
                dirty = false;
                transform.position = m.Position;
            }
            return;
        }
        if (m.Kind != EventKind.MoveResult || m.Revision < revision)
            return;
        if (m.Error != ResultCode.None || m.Revision > revision)
        {
            revision = m.Revision;
            sequence = MathfMax(sequence, m.Sequence);
            confirmed = m.Sequence;
            predicted.Clear();
            dirty = false;
            transform.position = m.Position;
            return;
        }
        if (m.Sequence <= confirmed)
            return;
        confirmed = m.Sequence;
        sequence = MathfMax(sequence, m.Sequence);
        if (predicted.TryGetValue(m.Sequence, out var atSequence))
        {
            Vector3 difference = m.Position - atSequence;
            if (difference.sqrMagnitude > .0001f)
                transform.position += difference;
        }
        var remove = new List<uint>();
        foreach (var key in predicted.Keys)
            if (key <= confirmed)
                remove.Add(key);
        foreach (var key in remove)
            predicted.Remove(key);
    }
    static uint MathfMax(uint a, uint b) => a > b ? a : b;
}
}
