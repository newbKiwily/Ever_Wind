using System.Collections.Generic;

using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class AlivePresentation
{
    private System.Action<Vector3, ServerMessage> reportDamage;
    private System.Action<int, Vector3> playSwing;
    private readonly AliveServerTransmitter actor;

    private readonly ActionVisualProfile profile;

    private readonly Animator animator;
    private readonly Vector3 visualLocalPosition;

    private readonly HashSet<ulong> seen = new HashSet<ulong>();

    private readonly Queue<ulong> seenOrder = new Queue<ulong>();

    private Vector3 lastPosition;

    private ulong action;

    private ActionKind kind;

    private int skill;

    private SkillDefinition skillDefinition;

    private double start, hitUntil, knockbackUntil;
    private double successUntil;

    private float duration;
    private float hitDuration;
    private float knockbackDuration;

    private bool swing;
    private bool castPlayed;
    private GameObject castVisual;
    private GameObject roVisual;
    private Vector3 roCenter;
    private Quaternion roRotation;
    private AnimatorOverrideController skillController;

    public uint PresentedActionCount { get; private set; }
    public bool HasAction => action != 0;
    public void Dispose()
    {
        reportDamage = null;
        playSwing = null;
        ClearVisuals();
    }

    private void ClearVisuals()
    {
        ClearCastEffect();
        if (roVisual != null) Object.Destroy(roVisual);
        roVisual = null;
        if (skillController != null) Object.Destroy(skillController);
        skillController = null;
    }
    public bool IsKnockedBack => actor.Mode != PlayerMode.Dead && (actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble) < knockbackUntil;

    public void InterruptSuccess()
    {
        successUntil = 0;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("Success", false);
        }
    }

    public AlivePresentation(AliveServerTransmitter actor, ActionVisualProfile profile, Animator animator, System.Action<Vector3, ServerMessage> reportDamage = null, System.Action<int, Vector3> playSwing = null)
    {
        this.reportDamage = reportDamage;
        this.playSwing = playSwing;
        this.actor = actor;

        this.profile = profile;

        this.animator = animator;
        visualLocalPosition = animator != null ? animator.transform.localPosition : Vector3.zero;

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
        ClearVisuals();
        seen.Clear();
        seenOrder.Clear();

        action = 0;

        kind = ActionKind.None;

        skill = 0;
        skillDefinition = null;

        start = 0;
        hitUntil = 0;
        knockbackUntil = 0;
        successUntil = 0;

        duration = 0;

        hitDuration = 0;
        knockbackDuration = 0;

        swing = false;

        lastPosition = position;

        PresentedActionCount = 0;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (profile != null) animator.runtimeAnimatorController = profile.Controller;
            animator.transform.localPosition = visualLocalPosition;
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
            InterruptSuccess();
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
            ClearCastEffect();
            if (roVisual != null) Object.Destroy(roVisual);
            roVisual = null;
            castPlayed = false;
            roCenter = message.Velocity;

            action = message.ActionId;

            kind = (ActionKind)message.Code;

            skill = message.Definition;
            // 채집의 아이템 ID와 제작법 ID를 공격 스킬 ID로 해석하지 않는다.
            skillDefinition = kind == ActionKind.Attack && actor.Kind == EntityKind.Player ? actor.Registry?.Catalog?.Skill(skill) : null;

            start = message.Value;

            duration = message.Duration;

            swing = false;

            if (animator != null)
            {
                if (profile != null) animator.runtimeAnimatorController = profile.Controller;
                if (skillDefinition != null && skillDefinition.Animation != null && profile != null)
                {
                    if (skillController != null) Object.Destroy(skillController);
                    var source = profile.Controller as AnimatorOverrideController;
                    skillController = new AnimatorOverrideController(source != null ? source.runtimeAnimatorController : profile.Controller);
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    if (source != null) source.GetOverrides(overrides); else skillController.GetOverrides(overrides);
                    for (int i = 0; i < overrides.Count; ++i)
                        if (overrides[i].Key.name == StateName()) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, skillDefinition.Animation);
                    skillController.ApplyOverrides(overrides);
                    animator.runtimeAnimatorController = skillController;
                }
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

        if (message.Kind == EventKind.ActionStarted) roRotation = actor.transform.rotation;
        if ((message.Kind == EventKind.ActionEnded || message.Kind == EventKind.ActionCancelled) && action == message.ActionId)
        {
            ClearCastEffect();
            if (roVisual != null) Object.Destroy(roVisual);
            roVisual = null;
            if (message.Kind == EventKind.ActionEnded && (kind == ActionKind.Gather || kind == ActionKind.Craft))
            {
                var success = profile != null ? profile.ResolveClip("Success") : null;
                successUntil = (actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble) + (success != null ? success.length : 0);
            }

            action = 0;

            if (animator != null)
            {
                animator.SetBool("ActionRunning", false);

                animator.ResetTrigger("ActionStart");
            }
        }

        double now = actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble;

        if (message.Mode == PlayerMode.Dead && (message.Kind == EventKind.Hit || message.Kind == EventKind.State))
            ClearCastEffect();

        if (message.Kind == EventKind.State || message.Kind == EventKind.Spawn)
        {
            bool knocked = (message.Flags & (ushort)HitReaction.Knockback) != 0;

            knockbackUntil = knocked ? message.ServerTime + message.HitTime : 0;

            if (knocked && knockbackDuration <= 0)
            {
                knockbackDuration = message.HitTime;
            }

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
            InterruptSuccess();
            if (message.Mode == PlayerMode.Dead)
            {
                hitUntil = 0;
                knockbackUntil = 0;
            }
            else if ((message.Flags & (ushort)HitReaction.Knockback) != 0)
            {
                hitUntil = 0;

                knockbackUntil = message.ServerTime + message.Duration;
                knockbackDuration = message.Duration;

                var pushDirection = message.Velocity;
                pushDirection.y = 0;

                if (pushDirection.sqrMagnitude > .001f)
                {
                    // 실제 밀림의 반대쪽을 바라봐 뒤로 밀리는 동작과 방향을 맞춘다.
                    actor.transform.rotation = Quaternion.LookRotation(-pushDirection);
                }

                animator?.ResetTrigger("HitStart");
            }
            else if ((message.Flags & (ushort)HitReaction.DamageOnly) != 0)
            {
                // 공격 중 피해 전용 반응은 남아 있던 일반 피격 모션도 종료한다.
                // 이미 진행 중인 넉백의 종료 시각과 표현은 유지한다.
                if (now >= knockbackUntil)
                {
                    hitUntil = 0;

                    animator?.ResetTrigger("HitStart");
                }
            }
            else if (now >= knockbackUntil)
            {
                hitDuration = message.Duration;

                hitUntil = message.ServerTime + message.Duration;

                if (animator != null && now < hitUntil)
                {
                    animator.ResetTrigger("HitStart");

                    animator.SetTrigger("HitStart");
                }
            }

            SpawnEffect(profile != null ? profile.HitEffect : null);

            reportDamage?.Invoke(actor.transform.position, message);
        }
    }

    public void Tick(float dt)
    {
        double now = actor.Transport != null ? actor.Transport.EstimatedServerTime : Time.unscaledTimeAsDouble;

        if (action == 0 || now >= start + duration || actor.Mode == PlayerMode.Dead)
            ClearCastEffect();

        Vector3 position = actor.transform.position;

        bool moving = (position - lastPosition).sqrMagnitude > .000001f;

        lastPosition = position;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            bool knocked = actor.Mode != PlayerMode.Dead && now < knockbackUntil;

            animator.SetFloat("Speed", moving ? 1 : 0);

            animator.SetBool("Combat", actor.Mode == PlayerMode.Combat);
            if (moving || action != 0 || actor.Mode != PlayerMode.NonCombat) successUntil = 0;
            animator.SetBool("Success", now < successUntil);
            animator.SetBool("Dead", actor.Mode == PlayerMode.Dead);
            if (profile != null && profile.DeadHeightOffset != 0)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                float blend = actor.Mode == PlayerMode.Dead && state.IsName(profile.Dead) ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(state.normalizedTime)) : 0;
                float parentScale = animator.transform.parent != null ? animator.transform.parent.lossyScale.y : 1;
                // 시각 모델만 내려 서버 좌표와 이동/충돌 판정에는 영향을 주지 않는다.
                animator.transform.localPosition = visualLocalPosition + Vector3.up * (profile.DeadHeightOffset * blend / Mathf.Max(.001f, parentScale));
            }
            animator.SetBool("Knockback", knocked);
            animator.SetBool("Hit", actor.Mode != PlayerMode.Dead && !knocked && now < hitUntil);

            var hitClip = profile != null ? profile.ResolveClip("Hit") : null;

            animator.SetFloat("HitSpeed", hitClip != null && hitDuration > 0 ? hitClip.length / hitDuration : 1);

            var knockbackClip = profile != null ? profile.ResolveClip("Knockback") : null;

            animator.SetFloat("KnockbackSpeed", knockbackClip != null && knockbackDuration > 0 ? knockbackClip.length / knockbackDuration : 1);
            animator.SetBool("ActionRunning", action != 0);

            var clip = action != 0 && kind == ActionKind.Attack && skillDefinition != null && skillDefinition.Animation != null ? skillDefinition.Animation : profile != null ? profile.ResolveClip(StateName()) : null;

            animator.SetFloat("ActionSpeed", clip != null && duration > 0 ? clip.length / duration : 1);
        }

        if (action != 0 && kind == ActionKind.Attack && skillDefinition != null)
        {
            float scale = skillDefinition.AuthoredTiming ? duration / skillDefinition.AttackDuration : 1;
            if (!castPlayed && now - start >= skillDefinition.CastEffectTime * scale)
            {
                castPlayed = true;
                if (now < start + duration && actor.Mode != PlayerMode.Dead)
                    castVisual = SpawnEffect(skillDefinition.CastEffect, followCaster: true, offset: skillDefinition.CastEffectOffset, rotation: skillDefinition.CastEffectRotation);
            }
            if (skillDefinition.UsesRO && skillDefinition.RangeDetector != null)
            {
                float spawn = skillDefinition is RangedSkill ranged ? ranged.SpawnTime * scale : 0;
                if (roVisual == null && now - start >= spawn)
                {
                    roVisual = Object.Instantiate(skillDefinition.RangeDetector.gameObject, roCenter, roRotation);
                    foreach (var a in roVisual.GetComponentsInChildren<Animator>(true)) { a.applyRootMotion = false; a.fireEvents = false; }
                }
                if (roVisual != null) roVisual.transform.position = roCenter;
            }
        }
        if (action != 0 && kind == ActionKind.Attack && !swing && now - start >= (skillDefinition != null ? skillDefinition.EffectDelay * (skillDefinition.AuthoredTiming ? duration / skillDefinition.AttackDuration : 1) : profile != null ? profile.SwingCue : .2f))
        {
            swing = true;

            if (skillDefinition != null && skillDefinition.SwingEffect != null)
            {
                SpawnEffect(skillDefinition.SwingEffect, skillDefinition.UsesRO ? (Vector3?)(roVisual != null ? roVisual.transform.position : roCenter) : null, offset: skillDefinition.SwingEffectOffset, rotation: skillDefinition.SwingEffectRotation);
            }
            else if (playSwing != null && actor.Kind == EntityKind.Player)
            {
                playSwing(skill, position);
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

    private void ClearCastEffect()
    {
        if (castVisual == null) return;
        castVisual.SetActive(false);
        Object.Destroy(castVisual);
        castVisual = null;
    }

    private GameObject SpawnEffect(GameObject prefab, Vector3? at = null, bool followCaster = false, Vector3 offset = default, Vector3 rotation = default)
    {
        if (prefab == null)
        {
            return null;
        }

        var instance = followCaster ? Object.Instantiate(prefab, actor.transform, false)
            : Object.Instantiate(prefab, (at ?? actor.transform.position + (profile != null ? profile.EffectOffset : Vector3.up)) + actor.transform.TransformDirection(offset), actor.transform.rotation * Quaternion.Euler(rotation));
        if (followCaster)
        {
            instance.transform.localPosition = (profile != null ? profile.EffectOffset : Vector3.up) + offset;
            instance.transform.localRotation = Quaternion.Euler(rotation);
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
            { var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.Local; }
        }

        if (!followCaster) Object.Destroy(instance, 3);
        return instance;
    }
}
}
