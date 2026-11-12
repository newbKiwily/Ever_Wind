using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class EntityPresentation : MonoBehaviour, IServerMessageHandler
{
    public ServerTransceiver Actor;
    public ActionVisualProfile Profile;
    public Animator Animator;
    Vector3 destination, lastPosition;
    bool havePosition;
    ulong action;
    ActionKind kind;
    int skill;
    double start, end;
    readonly HitAction hitReaction = new HitAction();
    ulong hitGeneration;
    float duration, hitTime;
    public uint PresentedActionCount { get; private set; }
    public float Progress => action != 0 &&
                             duration >
                                 0? Mathf.Clamp01((float)((Actor.Transport.EstimatedServerTime - start) /
                                                          duration)) : 0;
    public bool HasAction => action != 0;
    bool swing;
    string playing;
    Renderer[] resourceRenderers;
    int displayedStock = int.MinValue;
    void Awake()
    {
        resourceRenderers = GetComponentsInChildren<Renderer>();
    }
    readonly HashSet<ulong> seen = new HashSet<ulong>();
    readonly Queue<ulong> seenOrder = new Queue<ulong>();
    public void Handle(ServerMessage m)
    {
        if (m.Kind == EventKind.State || m.Kind == EventKind.MoveResult || m.Kind == EventKind.Hit)
        {
            destination = m.Position;
            havePosition = true;
        }
        if (m.Kind == EventKind.ActionStarted)
        {
            if (!seen.Add(m.ActionId))
                return;
            ++PresentedActionCount;
            seenOrder.Enqueue(m.ActionId);
            if (seenOrder.Count > 256)
                seen.Remove(seenOrder.Dequeue());
            action = m.ActionId;
            kind = (ActionKind)m.Code;
            skill = m.Definition;
            start = m.Value;
            duration = m.Duration;
            hitTime = m.HitTime;
            end = start + duration;
            swing = false;
            playing = null;
            if (Actor.Registry.TryGet(m.Target, out var target))
            {
                var d = target.transform.position - transform.position;
                d.y = 0;
                if (d.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.LookRotation(d);
            }
        }
        if ((m.Kind == EventKind.ActionEnded || m.Kind == EventKind.ActionCancelled) && m.ActionId == action)
        {
            action = 0;
            playing = null;
        }
        if (m.Kind == EventKind.Hit)
        {
            hitReaction.Begin(++hitGeneration);
            Play(Profile != null ? Profile.Damaged : "Damaged", 0);
            SpawnEffect(Profile != null ? Profile.HitEffect : null);
            if (GameplayIntegration.Active != null)
                GameplayIntegration.Active.ShowDamage(Actor, m);
            else
                GameplayVisualCue.Damage(transform.position + Vector3.up * 2, m.Value);
        }
    }
    void Update()
    {
        if (Actor == null || Actor.Transport == null)
            return;
        if (Actor.Kind == EntityKind.Resource)
        {
            if (displayedStock != Actor.Stock)
            {
                displayedStock = Actor.Stock;
                foreach (var renderer in resourceRenderers)
                {
                    renderer.enabled = Actor.Stock > 0;
                    renderer.SetPropertyBlock(null);
                }
                foreach (var collider in GetComponents<Collider>())
                    collider.enabled = Actor.Stock > 0;
            }
            return;
        }
        if (!Actor.IsLocal && havePosition)
        {
            transform.position =
                Vector3.Lerp(transform.position, destination, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
            var d = destination - lastPosition;
            d.y = 0;
            if (d.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.LookRotation(d);
        }
        bool moving = (transform.position - lastPosition).sqrMagnitude > .000001f;
        lastPosition = transform.position;
        double now = Actor.Transport.EstimatedServerTime;
        if (Actor.State.Current.Mode == PlayerMode.Dead)
        {
            hitReaction.Finish(ActionResult.Cancelled);
            Play(Profile != null ? Profile.Dead : "Dead", 0);
            return;
        }
        if (hitReaction.Result == ActionResult.Running)
        {
            hitReaction.Tick(Time.unscaledDeltaTime);
            if (hitReaction.Result == ActionResult.Running)
                return;
            playing = null;
        }
        if (action != 0)
        {
            string state = kind == ActionKind.Attack ? AttackState()
                           : Profile != null         ? Profile.Gather
                                                     : "Gather";
            float normalized = duration > 0 ? Mathf.Clamp01((float)((now - start) / duration)) : 0;
            Play(state, normalized);
            // Remap each half of the clip so its configured contact pose meets the server impact time.
            if (kind == ActionKind.Attack && Animator != null &&
                Animator.HasState(0, Animator.StringToHash(state)))
            {
                float elapsed = Mathf.Max(0, (float)(now - start));
                float contact = Profile != null ? Profile.AttackImpactNormalized : .45f;
                float phase = elapsed < hitTime ? contact * elapsed / Mathf.Max(.001f, hitTime)
                                                : contact + (1 - contact) * (elapsed - hitTime) /
                                                                Mathf.Max(.001f, duration - hitTime);
                Animator.speed = 0;
                Animator.Play(state, 0, Mathf.Clamp01(phase));
            }
            if (kind == ActionKind.Attack && !swing &&
                now - start >= (Profile != null ? Profile.SwingCue : .2f))
            {
                swing = true;
                if (GameplayIntegration.Active != null)
                    GameplayIntegration.Active.PlaySwing(skill, transform.position);
                else
                    SpawnEffect(Profile != null ? Profile.SwingEffect : null);
                if (GameplayIntegration.Active == null && (Profile == null || Profile.SwingEffect == null))
                    GameplayVisualCue.Swing(transform);
            }
            return; // End of animation never changes gameplay state; wait for server event.
        }
        string locomotion = Actor.State.Current.Locomotion(moving);
        if (Profile != null)
        {
            if (locomotion == "Idle")
                locomotion = Profile.Idle;
            else if (locomotion == "Run")
                locomotion = Profile.Run;
            else if (locomotion == "CombatIdle")
                locomotion = Profile.CombatIdle;
            else if (locomotion == "CombatRun")
                locomotion = Profile.CombatRun;
        }
        Play(locomotion, 0);
    }
    string AttackState() => Profile != null
                            && Profile.Attacks.Length >
                                   0? Profile.Attacks[Mathf.Clamp(skill - 1, 0, Profile.Attacks.Length - 1)]
        : "Attack1";
    void Play(string state, float offset)
    {
        if (Animator == null || playing == state || !Animator.HasState(0, Animator.StringToHash(state)))
            return;
        Animator.speed =
            action != 0 && duration > 0 && Profile != null ? Profile.Length(state) / duration : 1;
        Animator.CrossFade(state, .05f, 0, offset);
        playing = state;
    }
    void SpawnEffect(GameObject prefab)
    {
        if (prefab == null)
            return;
        var instance =
            Instantiate(prefab, transform.position + (Profile != null ? Profile.EffectOffset : Vector3.up),
                        Quaternion.identity);
        Destroy(instance, 3);
    }
}
}
