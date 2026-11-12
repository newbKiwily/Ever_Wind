using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverWind.Gameplay
{
public sealed class PlayerActionController : MonoBehaviour, IServerMessageHandler
{
    public ServerTransceiver Actor;
    public MovementMotor Motor;
    public ServerTransceiver SelectedTarget { get; private set; }
    public string LastResult { get; private set; } = "Idle";
    public bool AutoApproach = true;
    public float TargetRadius = 50;
    readonly List<ServerTransceiver> targetCandidates = new List<ServerTransceiver>();
    public void SelectTarget(ServerTransceiver target)
    {
        SelectedTarget = target;
    }
    PlayerAction current;
    MoveAction manual;
    ulong generation;
    int intentTarget, intentSkill;
    RequestKind intentKind;
    bool hasIntent, waiting, cancelPending;
    ulong pendingActionRequest;
    readonly HashSet<ulong> abandonedRequests = new HashSet<ulong>();
    Action afterCancel;
    float requestAge;
    Vector3 lastInput;
    void Awake()
    {
        manual = new MoveAction(GetComponent<MovementMotor>());
    }
    void Update()
    {
        if (Actor == null || !Actor.IsLocal || !Actor.Transport.Ready)
            return;
        requestAge += Time.unscaledDeltaTime;
        if (requestAge > 6 && waiting)
        {
            waiting = false;
            LastResult = "Request timed out";
            AbandonIntent();
        }
        if (Actor.State.Current.Mode == PlayerMode.Dead)
        {
            ClearIntent();
            StopCurrent();
            return;
        }
        // Input is processed before action completion, so direct input wins arrival in the same frame.
        bool acceptInput = !GameplayInput.IsTyping;
        Vector3 direction = acceptInput ? GameplayInput.ReadMovement() : Vector3.zero;
        if (direction.sqrMagnitude > .01f)
        {
            if (!(current is KnockbackAction))
            {
                if (hasIntent)
                {
                    AbandonIntent();
                }
                if (Actor.ActionId != 0)
                {
                    if (!cancelPending)
                        CancelServer(null);
                }
                else if (Actor.State.Current.Allows(ActionKind.None))
                {
                    if (current != manual)
                    {
                        StopCurrent();
                        current = manual;
                        manual.Begin(++generation);
                    }
                    manual.Direction = direction;
                }
            }
        }
        else if (current == manual)
        {
            StopCurrent();
        }
        lastInput = direction;
        if (acceptInput && Input.GetKeyDown(KeyCode.Escape) &&
            (GameplayIntegration.Active == null ||
             (!GameplayIntegration.Active.Popups.IsPopUpOn &&
              GameplayIntegration.Active.EscapeConsumedFrame != Time.frameCount)))
        {
            Interrupt();
        }
        // Left click enters combat as before; it never raycasts/selects an object.
        if (acceptInput && !GameplayInput.IsPointerOverUI &&
            (GameplayInput.Legacy != null ? GameplayInput.Legacy.GetEnterCombatDown()
                                          : Input.GetMouseButtonDown(0)) &&
            Actor.State.Current.Mode == PlayerMode.NonCombat)
            RequestMode(PlayerMode.Combat);
        if (acceptInput &&
            (GameplayInput.Legacy != null ? GameplayInput.Legacy.GetChangeTargetDown()
                                          : Input.GetKeyDown(KeyCode.E)) &&
            Actor.State.Current.Mode == PlayerMode.Combat)
            ChangeTarget();
        int attackKey = GameplayInput.Legacy != null ? GameplayInput.Legacy.GetAttackKeyDown() : 0;
        if (acceptInput)
        {
            for (int i = 1; i <= 5; i++)
                if (GameplayInput.Legacy != null ? attackKey == i
                                                 : Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
                {
                    if (Actor.Registry.Catalog.HasSkill(i))
                        RequestAttack(i);
                }
        }
        if (acceptInput && (GameplayInput.Legacy != null ? GameplayInput.Legacy.GetInteractDown()
                                                         : Input.GetKeyDown(KeyCode.Space)))
            RequestGather();
        if (acceptInput && GameplayIntegration.Active == null && Input.GetKeyDown(KeyCode.C))
            RequestCraft(1);
        if (acceptInput && Input.GetKeyDown(KeyCode.R))
            RequestMode(Actor.State.Current.Mode == PlayerMode.Combat ? PlayerMode.NonCombat
                                                                      : PlayerMode.Combat);
        var executing = current;
        if (executing == null)
            return;
        ulong id = executing.ExecutionId;
        executing.Tick(Time.unscaledDeltaTime);
        if (current != executing || id != executing.ExecutionId || executing.Result == ActionResult.Running)
            return;
        current = null;
        Motor.Forced = false;
        LastResult = executing.Result.ToString();
        if (executing.Result == ActionResult.ReachedTarget && hasIntent)
            TryIntent();
        else if (executing is ApproachTargetAction)
            ClearIntent();
    }
    public void SelectNearest()
    {
        RefreshTargets();
        SelectTarget(targetCandidates.Count > 0 ? targetCandidates[0] : null);
        FaceSelectedTarget();
    }
    void RefreshTargets()
    {
        targetCandidates.Clear();
        foreach (var target in Actor.Registry.Entities)
        {
            if (IsAvailableMonster(target))
                targetCandidates.Add(target);
        }
        targetCandidates.Sort((a, b) =>
                              {
                                  int order =
                                      (a.transform.position - transform.position)
                                          .sqrMagnitude.CompareTo(
                                              (b.transform.position - transform.position).sqrMagnitude);
                                  return order != 0 ? order : a.EntityId.CompareTo(b.EntityId);
                              });
    }
    bool IsAvailableMonster(ServerTransceiver target) =>
        target != null && target.Kind == EntityKind.Monster && target.Hp > 0 &&
        (target.transform.position - transform.position).sqrMagnitude <= TargetRadius *TargetRadius;
    public void ChangeTarget()
    {
        RefreshTargets();
        int index = targetCandidates.IndexOf(SelectedTarget);
        SelectTarget(targetCandidates.Count > 0 ? targetCandidates[(index + 1) % targetCandidates.Count]
                                                : null);
        FaceSelectedTarget();
        if (hasIntent && intentKind == RequestKind.Attack && current is ApproachTargetAction)
        {
            if (SelectedTarget != null)
                BeginIntent(RequestKind.Attack, SelectedTarget.EntityId, intentSkill);
            else
                Interrupt();
        }
    }
    void FaceSelectedTarget()
    {
        if (SelectedTarget == null || Actor.ActionId != 0)
            return;
        var direction = SelectedTarget.transform.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
    public void RequestAttack(int skill)
    {
        if (!IsAvailableMonster(SelectedTarget))
            SelectNearest();
        if (SelectedTarget == null)
            return;
        BeginIntent(RequestKind.Attack, SelectedTarget.EntityId, skill);
    }
    public void RequestGather()
    {
        if (SelectedTarget == null || SelectedTarget.Kind != EntityKind.Resource)
        {
            ServerTransceiver nearest = null;
            float best = TargetRadius * TargetRadius;
            foreach (var target in Actor.Registry.Entities)
            {
                if (target.Kind != EntityKind.Resource || target.Stock <= 0)
                    continue;
                float distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance < best)
                {
                    nearest = target;
                    best = distance;
                }
            }
            if (nearest != null)
                BeginIntent(RequestKind.Gather, nearest.EntityId, 0);
            else
                LastResult = "No nearby resource";
            return;
        }
        BeginIntent(RequestKind.Gather, SelectedTarget.EntityId, 0);
    }
    public void RequestCraft(int recipe)
    {
        BeginIntent(RequestKind.Craft, 0, recipe);
    }
    void BeginIntent(RequestKind kind, int target, int definition)
    {
        if (current is KnockbackAction || Actor.State.Current.Mode == PlayerMode.Dead)
            return;
        if (waiting || abandonedRequests.Count != 0)
        {
            LastResult = "Waiting for authoritative action";
            return;
        }
        if (Actor.ActionId != 0)
        {
            CancelServer(() => BeginIntent(kind, target, definition));
            return;
        }
        StopCurrent();
        ClearIntent();
        intentKind = kind;
        intentTarget = target;
        intentSkill = definition;
        hasIntent = true;
        if (kind == RequestKind.Attack && Actor.State.Current.Mode == PlayerMode.NonCombat)
        {
            ulong token = generation;
            waiting = true;
            requestAge = 0;
            Actor.Send(new Request { Kind = RequestKind.Mode, Definition = (int)PlayerMode.Combat },
                       m =>
                       {
                           if (this == null || token != generation)
                               return;
                           waiting = false;
                           if (m.Error == ResultCode.None)
                               TryIntent();
                           else
                               Fail(m);
                       });
        }
        else
            TryIntent();
    }
    void TryIntent()
    {
        if (!hasIntent)
            return;
        ActionKind type = intentKind == RequestKind.Attack   ? ActionKind.Attack
                          : intentKind == RequestKind.Gather ? ActionKind.Gather
                                                             : ActionKind.Craft;
        if (!Actor.State.Current.Allows(type))
        {
            LastResult = "Blocked by " + Actor.State.Current.Mode;
            ClearIntent();
            return;
        }
        ServerTransceiver target = null;
        if (intentTarget != 0 && !Actor.Registry.TryGet(intentTarget, out target))
        {
            ClearIntent();
            return;
        }
        float reach = intentKind == RequestKind.Attack ? Actor.Registry.SkillReach(intentSkill) : 1.2f;
        if (target != null)
        {
            float range = Motor.Radius + target.Radius + reach;
            var d = target.transform.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > range * range)
            {
                if (!AutoApproach)
                {
                    LastResult = "OutOfRange";
                    ClearIntent();
                    return;
                }
                StopCurrent();
                current = new ApproachTargetAction(Motor, target, Mathf.Max(0, reach - .08f));
                current.Begin(++generation);
                return;
            }
        }
        Motor.FlushPending();
        ulong token = generation;
        waiting = true;
        requestAge = 0;
        Action<ServerMessage> cb = m =>
        {
            if (m.Error != ResultCode.None && m.Error != ResultCode.Expired)
                abandonedRequests.Remove(m.RequestId);
            if (this == null || token != generation)
                return;
            waiting = false;
            if (m.Error != ResultCode.None)
                Fail(m);
            else
            {
                LastResult = "Approved; awaiting action event";
                hasIntent = false;
            }
        };
        if (intentKind == RequestKind.Attack)
        {
            var attack = new AttackAction(Actor, target, intentSkill, reach);
            var result = attack.TryRequest(cb);
            pendingActionRequest = attack.RequestId;
            if (result != ActionResult.Waiting)
            {
                waiting = false;
                LastResult = result.ToString();
                ClearIntent();
            }
        }
        else if (intentKind == RequestKind.Gather)
            pendingActionRequest = new ObtainAction(Actor, intentTarget).Request(cb);
        else
            pendingActionRequest = new CraftAction(Actor, intentSkill).Request(cb);
    }
    void Fail(ServerMessage m)
    {
        LastResult = m.Error.ToString();
        if (m.Error == ResultCode.Expired)
        {
            AbandonIntent();
            Actor.Transport
                .CloseConnection(); // Reconnect instead of guessing whether a timed-out action started.
        }
        else
            ClearIntent();
    }
    void ClearIntent()
    {
        hasIntent = false;
        waiting = false;
        pendingActionRequest = 0;
        ++generation;
    }
    void AbandonIntent()
    {
        if (pendingActionRequest != 0)
            abandonedRequests.Add(pendingActionRequest);
        ClearIntent();
    }
    public void Interrupt()
    {
        AbandonIntent();
        if (!(current is KnockbackAction))
            StopCurrent();
        if (Actor != null && Actor.ActionId != 0)
            CancelServer(null);
    }
    void StopCurrent()
    {
        current?.Finish(ActionResult.Cancelled);
        current = null;
        if (Motor != null)
            Motor.Forced = false;
    }
    void RequestMode(PlayerMode mode)
    {
        if (Actor.ActionId != 0 || waiting)
            return;
        Actor.Send(new Request { Kind = RequestKind.Mode, Definition = (int)mode },
                   m =>
                   {
                       if (m.Error != ResultCode.None)
                           LastResult = m.Error.ToString();
                   });
    }
    void CancelServer(Action next)
    {
        if (cancelPending)
            return;
        ClearIntent();
        StopCurrent();
        cancelPending = true;
        afterCancel = next;
        ulong token = generation;
        Actor.Send(new Request { Kind = RequestKind.Cancel, ActionId = Actor.ActionId },
                   m =>
                   {
                       if (this == null)
                           return;
                       cancelPending = false;
                       if (token != generation)
                       {
                           afterCancel = null;
                           return;
                       }
                       var continuation = afterCancel;
                       afterCancel = null;
                       if (m.Error == ResultCode.None)
                           continuation?.Invoke();
                       else
                           LastResult = m.Error.ToString();
                   });
    }
    public void Handle(ServerMessage m)
    {
        if (m.Kind == EventKind.ActionStarted)
        {
            StopCurrent();
            ClearIntent();
            LastResult = "Action " + m.ActionId + ": " + (ActionKind)m.Code;
            if ((ActionKind)m.Code == ActionKind.Attack && Actor.Registry.TryGet(m.Target, out var target))
                current =
                    new AttackAction(Actor, target, m.Definition, Actor.Registry.SkillReach(m.Definition));
            else if ((ActionKind)m.Code == ActionKind.Gather)
                current = new ObtainAction(Actor, m.Target);
            else if ((ActionKind)m.Code == ActionKind.Craft)
                current = new CraftAction(Actor, m.Definition);
            if (current != null)
            {
                current.Begin(generation);
                current.AttachServerAction(m.ActionId);
            }
            if (abandonedRequests.Remove(m.RequestId))
            {
                CancelServer(null);
            }
        }
        if (m.Kind == EventKind.Hit && (m.Flags & 1) != 0)
        {
            ClearIntent();
            StopCurrent();
            waiting = false;
            current = new KnockbackAction(Motor, m.Velocity, m.Duration);
            current.Begin(++generation);
            Motor.Forced = true;
        }
        if (m.Mode == PlayerMode.Dead && (m.Kind == EventKind.State || m.Kind == EventKind.Hit))
        {
            ClearIntent();
            StopCurrent();
            waiting = false;
        }
        if (m.Kind == EventKind.ActionEnded || m.Kind == EventKind.ActionCancelled)
        {
            if (current != null && current.ServerActionId == m.ActionId)
            {
                current.Finish(m.Kind == EventKind.ActionEnded ? ActionResult.Completed
                                                               : ActionResult.Cancelled);
                current = null;
            }
            LastResult = m.Kind.ToString();
        }
    }
    void OnDisable()
    {
        ClearIntent();
        StopCurrent();
        afterCancel = null;
    }
}
}
