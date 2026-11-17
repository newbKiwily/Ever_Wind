using System.Collections.Generic;

using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class AlivePresentation
{
    private readonly AliveServerTransmitter actor;

    private readonly ActionVisualProfile profile;

    private readonly Animator animator;

    private readonly HashSet<ulong> seen = new HashSet<ulong>();

    private readonly Queue<ulong> seenOrder = new Queue<ulong>();

    private Vector3 lastPosition;

    private ulong action;

    private ActionKind kind;

    private int skill;

    private double start, hitUntil, knockbackUntil;

    private float duration;

    private bool swing;

    public uint PresentedActionCount { get; private set; }
    public bool HasAction => action != 0;

    public AlivePresentation(AliveServerTransmitter actor, ActionVisualProfile profile, Animator animator)
    {
        this.actor = actor;

        this.profile = profile;

        this.animator = animator;

        if (animator != null && profile != null)
        {
            animator.runtimeAnimatorController = profile.Controller;

            animator.applyRootMotion = false;

            animator.fireEvents = false;
        }

        lastPosition = actor.transform.position;
    }

    public void Handle(ServerMessage message)
    {
        if (message.Kind == EventKind.Spawn)
        {
            lastPosition = message.Position;
        }

        if (message.Kind == EventKind.ActionStarted)
        {
            if (!seen.Add(message.ActionId))
            {
                return;
            }

            seenOrder.Enqueue(message.ActionId);

            if (seenOrder.Count > 256)
            {
                seen.Remove(seenOrder.Dequeue());
            }

            ++PresentedActionCount;

            action = message.ActionId;

            kind = (ActionKind)message.Code;

            skill = message.Definition;

            start = message.Value;

            duration = message.Duration;

            swing = false;

            if (animator != null)
            {
                animator.SetInteger("ActionSlot", Slot());

                animator.SetBool("ActionRunning", true);

                animator.ResetTrigger("ActionStart");

                animator.SetTrigger("ActionStart");
            }

            if (actor.Registry != null && actor.Registry.TryGet(message.Target, out var target))
            {
                Vector3 direction = target.transform.position - actor.transform.position;

                direction.y = 0;

                if (direction.sqrMagnitude > .001f)
                {
                    actor.transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        if ((message.Kind == EventKind.ActionEnded || message.Kind == EventKind.ActionCancelled) && action == message.ActionId)
        {
            action = 0;

            if (animator != null)
            {
                animator.SetBool("ActionRunning", false);

                animator.ResetTrigger("ActionStart");
            }
        }

        if (message.Kind == EventKind.Hit)
        {
            double now = actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble;

            hitUntil = now + .18;

            if ((message.Flags & 1) != 0)
            {
                knockbackUntil = now + message.Duration;
            }

            SpawnEffect(profile != null ? profile.HitEffect : null);

            GameplayIntegration.Active?.ShowDamage(actor, message);
        }
    }

    public void Tick(float dt)
    {
        double now = actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble;

        Vector3 position = actor.transform.position;

        bool moving = (position - lastPosition).sqrMagnitude > .000001f;

        lastPosition = position;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            bool knocked = now < knockbackUntil;

            animator.SetFloat("Speed", moving ? 1 : 0);

            animator.SetBool("Combat", actor.Mode == PlayerMode.Combat);
            animator.SetBool("Dead", actor.Mode == PlayerMode.Dead);
            animator.SetBool("Knockback", knocked);
            animator.SetBool("Hit", !knocked && now < hitUntil);
            animator.SetBool("ActionRunning", action != 0);

            var clip = profile != null ? profile.ResolveClip(StateName()) : null;

            animator.SetFloat("ActionSpeed", clip != null && duration > 0 ? clip.length / duration : 1);
        }

        if (action != 0 && kind == ActionKind.Attack && !swing && now - start >= (profile != null ? profile.SwingCue : .2f))
        {
            swing = true;

            if (GameplayIntegration.Active != null)
            {
                GameplayIntegration.Active.PlaySwing(skill, position);
            }
            else
            {
                SpawnEffect(profile != null ? profile.SwingEffect : null);
            }
        }
    }

    private int Slot()
    {
        return kind == ActionKind.Attack ? Mathf.Clamp(skill, 1, 5) : kind == ActionKind.Craft ? 7
                                                                                               : 6;
    }

    private string StateName()
    {
        return Slot() <= 5 ? "Skill" + Slot() : Slot() == 7 ? "Craft"
                                                            : "Gather";
    }

    private void SpawnEffect(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        var instance = Object.Instantiate(prefab, actor.transform.position + (profile != null ? profile.EffectOffset : Vector3.up), Quaternion.identity);

        Object.Destroy(instance, 3);
    }
}
}
