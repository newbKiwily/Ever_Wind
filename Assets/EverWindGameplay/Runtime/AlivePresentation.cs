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

    private SkillDefinition skillDefinition;

    private double start, hitUntil, knockbackUntil;

    private float duration;
    private float hitDuration;

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

    private void ResetForSpawn(Vector3 position)
    {
        seen.Clear();
        seenOrder.Clear();

        action = 0;

        kind = ActionKind.None;

        skill = 0;
        skillDefinition = null;

        start = 0;
        hitUntil = 0;
        knockbackUntil = 0;

        duration = 0;

        hitDuration = 0;

        swing = false;

        lastPosition = position;

        PresentedActionCount = 0;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();

            animator.SetFloat("ActionSpeed", 1);
        }
    }

    public void Handle(ServerMessage message)
    {
        if (message.Kind == EventKind.Spawn)
        {
            ResetForSpawn(message.Position);
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
            skillDefinition = actor.Kind == EntityKind.Player ? actor.Registry?.Catalog?.Skill(skill) : null;

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

        double now = actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble;

        if (message.Kind == EventKind.State || message.Kind == EventKind.Spawn)
        {
            bool knocked = (message.Flags & (ushort)HitReaction.Knockback) != 0;

            knockbackUntil = knocked ? message.ServerTime + message.HitTime : 0;

            if ((message.Flags & (ushort)HitReaction.Hit) != 0)
            {
                hitUntil = message.ServerTime + message.HitTime;

                if (hitDuration <= 0)
                {
                    hitDuration = message.HitTime;
                }
            }
            else if (!knocked)
            {
                hitUntil = 0;
            }
        }

        if (message.Kind == EventKind.Hit)
        {
            if (message.Mode == PlayerMode.Dead)
            {
                hitUntil = 0;
                knockbackUntil = 0;
            }
            else if ((message.Flags & (ushort)HitReaction.Knockback) != 0)
            {
                hitUntil = 0;

                knockbackUntil = message.ServerTime + message.Duration;

                animator?.ResetTrigger("HitStart");
            }
            else if ((message.Flags & (ushort)HitReaction.DamageOnly) == 0 && now >= knockbackUntil)
            {
                hitDuration = message.Duration;

                hitUntil = message.ServerTime + message.Duration;

                if (animator != null && now < hitUntil)
                {
                    animator.ResetTrigger("HitStart");

                    animator.SetTrigger("HitStart");
                }
            }

            var hitDefinition = actor.Registry?.Catalog?.Skill(message.Definition);
            SpawnEffect(hitDefinition != null && hitDefinition.HitEffect != null ? hitDefinition.HitEffect : profile != null ? profile.HitEffect : null);

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
            bool knocked = actor.Mode != PlayerMode.Dead && now < knockbackUntil;

            animator.SetFloat("Speed", moving ? 1 : 0);

            animator.SetBool("Combat", actor.Mode == PlayerMode.Combat);
            animator.SetBool("Dead", actor.Mode == PlayerMode.Dead);
            animator.SetBool("Knockback", knocked);
            animator.SetBool("Hit", actor.Mode != PlayerMode.Dead && !knocked && now < hitUntil);

            var hitClip = profile != null ? profile.ResolveClip("Hit") : null;

            animator.SetFloat("HitSpeed", hitClip != null && hitDuration > 0 ? hitClip.length / hitDuration : 1);
            animator.SetBool("ActionRunning", action != 0);

            var clip = profile != null ? profile.ResolveClip(StateName()) : null;

            animator.SetFloat("ActionSpeed", clip != null && duration > 0 ? clip.length / duration : 1);
        }

        if (action != 0 && kind == ActionKind.Attack && !swing && now - start >= (skillDefinition != null ? skillDefinition.EffectDelay : profile != null ? profile.SwingCue : .2f))
        {
            swing = true;

            if (skillDefinition != null && skillDefinition.SwingEffect != null)
            {
                SpawnEffect(skillDefinition.SwingEffect);
            }
            else if (GameplayIntegration.Active != null && actor.Kind == EntityKind.Player)
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
        return kind == ActionKind.Attack ? Mathf.Clamp(skillDefinition != null ? skillDefinition.AnimationSlot : skill, 1, 5) : kind == ActionKind.Craft ? 7
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

        var instance = Object.Instantiate(prefab, actor.transform.position + (profile != null ? profile.EffectOffset : Vector3.up), actor.transform.rotation);

        Object.Destroy(instance, 3);
    }
}
}
