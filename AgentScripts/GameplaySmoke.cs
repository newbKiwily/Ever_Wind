using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EverWind.Gameplay;
using Object = UnityEngine.Object;
public static class GameplaySmoke
{
    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    static async Task Wait(Func<bool> predicate, string label, float timeout = 8)
    {
        float until = Time.realtimeSinceStartup + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup > until)
                throw new TimeoutException(label);
            await Task.Delay(25);
        }
    }
    sealed class Client
    {
        public ServerTransceiverManager Registry;
        public EverWind.Gameplay.NetworkClient Net;
        public uint Seq, Revision;
        public List<ServerMessage> Events = new List<ServerMessage>();
        public Client(ServerTransceiverManager registry)
        {
            Registry = registry;
            Net = registry.Transport;
            Net.Message += m =>
            {
                Events.Add(m);
                if (m.Entity == Net.LocalEntityId && (m.Kind == EventKind.MoveResult || m.Kind == EventKind.State))
                {
                    Seq = Math.Max(Seq, m.Sequence);
                    Revision = Math.Max(Revision, m.Revision);
                }
            };
        }
        public Task<ServerMessage> Send(Request r)
        {
            var completion = new TaskCompletionSource<ServerMessage>();
            Net.Send(r, m => completion.TrySetResult(m));
            return completion.Task;
        }
        public async Task MoveTo(Vector3 goal)
        {
            for (int i = 0; i < 200; i++)
            {
                var a = Registry.LocalActor;
                var delta = goal - a.transform.position;
                delta.y = 0;
                if (delta.magnitude < .08f)
                    return;
                await Task.Delay(100);
                var next = a.transform.position + Vector3.ClampMagnitude(delta, .4f);
                var ack = await Send(
                    new Request { Kind = RequestKind.Move, Sequence = ++Seq, Revision = Revision, Position = next });
                Check(ack.Error == ResultCode.None, "Move rejected: " + ack.Error);
                a.transform.position = ack.Position;
            }
            throw new Exception("Movement failed to finish");
        }
        public PlayerActionController Control => Registry.LocalActor.GetComponent<PlayerActionController>();
        public async Task Gather(int id)
        {
            Control.SelectTarget(null); // Space uses the original nearest-resource interaction rule.
            Control.RequestGather();
            await Wait(() => Registry.LocalActor.ActionId != 0, "Gather start");
            Check(Events.Last(e => e.Kind == EventKind.ActionStarted && e.Entity == Net.LocalEntityId).Target == id,
                  "Nearest resource selection mismatch");
            await Wait(() => Registry.LocalActor.ActionId == 0, "Gather end");
        }
    }
    public static async Task<string> Run()
    {
        Check(Application.isPlaying, "Enter play mode first");
        var source = Object.FindFirstObjectByType<ServerTransceiverManager>();
        Check(source != null, "Missing verification scene");
        var a = new Client(source);
        a.Net.Port = 14000;
        a.Net.Login("fixture-a", "test-only");
        await Wait(() => a.Net.Ready, "Client A login: " + a.Net.Status);
        var second = new GameObject("Second verification client");
        second.SetActive(false);
        var net = second.AddComponent<EverWind.Gameplay.NetworkClient>();
        net.Port = 14000;
        var registry = second.AddComponent<ServerTransceiverManager>();
        registry.Transport = net;
        registry.Catalog = source.Catalog;
        registry.PlayerVisual = source.PlayerVisual;
        registry.PlayerProfile = source.PlayerProfile;
        registry.MonsterVisuals = source.MonsterVisuals;
        registry.MonsterProfiles = source.MonsterProfiles;
        registry.ResourceVisual = source.ResourceVisual;
        registry.WallVisual = source.WallVisual;
        registry.TargetIndicatorVisual = source.TargetIndicatorVisual;
        second.SetActive(true);
        var b = new Client(registry);
        net.Login("fixture-b", "test-only");
        await Wait(() => net.Ready, "Client B login");
        foreach (var animator in source.GetComponentsInChildren<Animator>(true))
            Check(!animator.applyRootMotion && !animator.fireEvents, "Runtime animator retained authority");
        Check(a.Registry.TryGet(net.LocalEntityId, out _), "A cannot observe B");
        float authoritativeSpeed =
            a.Events.First(e => e.Kind == EventKind.Spawn && e.Entity == a.Net.LocalEntityId).Duration;
        Check(
            authoritativeSpeed > 0 &&
                Mathf.Approximately(a.Registry.LocalActor.GetComponent<MovementMotor>().MoveSpeed, authoritativeSpeed),
            "Server movement speed was not applied");
        var camera = Camera.main;
        var orbit = camera.GetComponent<GameplayCameraController>();
        Check(orbit != null && Mathf.Approximately(camera.fieldOfView, 60), "Legacy camera not configured");
        a.Control.SelectNearest();
        var firstTarget = a.Control.SelectedTarget;
        a.Control.ChangeTarget();
        Check(a.Control.SelectedTarget != firstTarget && a.Control.SelectedTarget.Kind == EntityKind.Monster,
              "E did not cycle targets");
        a.Control.ChangeTarget();
        Check(a.Control.SelectedTarget == firstTarget, "Target cycle did not wrap");
        Vector3 movement = GameplayInput.Movement(camera.transform, .25f, .5f);
        Check(Mathf.Abs(movement.magnitude - Mathf.Sqrt(.25f * .25f + .5f * .5f)) < .001f,
              "Movement axis magnitude was normalized");
        Quaternion beforeOrbit = camera.transform.rotation;
        for (int i = 0; i < 12; ++i)
            orbit.Orbit(new Vector2(0, -1), 0, true, .016f);
        orbit.Orbit(Vector2.zero, 100, false, .016f);
        Check(Mathf.Approximately(orbit.Distance, 8), "Legacy zoom-in clamp");
        orbit.Orbit(Vector2.zero, -100, false, .016f);
        Check(Mathf.Approximately(orbit.Distance, 20), "Legacy zoom-out clamp");
        await Task.Delay(100);
        Check(Quaternion.Angle(beforeOrbit, camera.transform.rotation) > 1, "Orbit did not rotate camera");
        Check(Mathf.Abs(Vector3.Distance(camera.transform.position, a.Registry.LocalActor.transform.position) - 20) <
                  .01f,
              "Camera follow offset changed");
        a.Registry.TryGet(10001, out var initialMonster);
        a.Control.SelectTarget(initialMonster);
        a.Control.RequestAttack(1); // Mode request is still pending when interrupted.
        a.Control.Interrupt();
        await Wait(() => a.Registry.LocalActor.State.Current.Mode == PlayerMode.Combat, "Delayed mode result");
        await Task.Delay(150);
        Check(!a.Events.Any(e => e.Kind == EventKind.ActionStarted && e.Entity == a.Net.LocalEntityId),
              "Interrupted mode request started an action");
        await a.Send(new Request { Kind = RequestKind.Mode, Definition = (int)PlayerMode.NonCombat });
        uint before = a.Revision;
        var bad = await a.Send(new Request { Kind = RequestKind.Move, Sequence = ++a.Seq, Revision = a.Revision,
                                             Position = new Vector3(100, 0, 100) });
        Check(bad.Error == ResultCode.Collision, "Boundary was not rejected");
        Check(a.Revision > before, "Missing correction revision");
        uint diagonalRevision = a.Revision;
        var diagonal = new MoveAction(a.Registry.LocalActor.GetComponent<MovementMotor>()) {
            Direction = GameplayInput.Movement(camera.transform, 1, 1)
        };
        diagonal.Begin(1);
        for (int i = 0; i < 6; ++i)
        {
            diagonal.Tick(.05f);
            await Task.Delay(60);
        }
        await Task.Delay(100);
        Check(a.Revision == diagonalRevision, "Legacy diagonal speed caused server correction");
        await a.MoveTo(new Vector3(-5, 0, -7));
        await b.MoveTo(new Vector3(-5, 0, -7));
        a.Registry.TryGet(20001, out var tree);
        a.Control.SelectTarget(tree);
        a.Control.RequestGather(); // Interrupt before the authoritative start event arrives.
        a.Control.Interrupt();
        await Wait(() => a.Events.Any(e => e.Kind == EventKind.ActionCancelled && e.Entity == a.Net.LocalEntityId),
                   "Late start cancellation");
        Check(a.Registry.LocalActor.ActionId == 0, "Interrupted request retained action ownership");
        a.Control.SelectTarget(tree);
        a.Control.RequestGather();
        await Wait(() => a.Registry.LocalActor.ActionId != 0, "First gather start");
        var busy = await b.Send(new Request { Kind = RequestKind.Gather, Target = 20001 });
        Check(busy.Error == ResultCode.Busy, "Resource double ownership");
        var cancelled =
            await a.Send(new Request { Kind = RequestKind.Cancel, ActionId = a.Registry.LocalActor.ActionId });
        Check(cancelled.Error == ResultCode.None, "Cancel rejected");
        await Task.Delay(2200);
        Check(!a.Registry.Inventory.ContainsKey(1), "Cancelled gather rewarded");
        await a.Gather(20001);
        await a.Gather(20001);
        Check(a.Registry.Inventory[1] == 2, "Gather amount mismatch");
        a.Control.RequestCraft(2);
        await Wait(() => a.Registry.LocalActor.ActionId != 0, "Craft start");
        await Wait(() => a.Registry.LocalActor.ActionId == 0, "Craft end");
        Check(a.Registry.Inventory.TryGetValue(4, out int amount) && amount == 1,
              "Existing AdvancedBranch recipe not applied");
        Check(a.Registry.Inventory[1] == 0, "Ingredients not consumed");
        await a.MoveTo(new Vector3(2, 0, -7));
        await a.Gather(20003);
        a.Control.RequestCraft(1);
        await Wait(() => a.Registry.LocalActor.ActionId != 0, "CampFire start");
        await Wait(() => a.Registry.LocalActor.ActionId == 0, "CampFire end");
        Check(a.Registry.Inventory.TryGetValue(3, out amount) && amount == 1, "Existing CampFire recipe not applied");
        await a.MoveTo(new Vector3(7, 0, -7));
        await a.MoveTo(new Vector3(7, 0, -2));
        a.Registry.TryGet(10001, out var monster);
        Check(monster != null, "Monster missing");
        float hp = monster.Hp;
        uint ownPresentation = a.Registry.LocalActor.GetComponent<EntityPresentation>().PresentedActionCount;
        Check(b.Registry.TryGet(a.Net.LocalEntityId, out var observedPlayer), "B cannot observe player A");
        uint remotePresentation = observedPlayer.GetComponent<EntityPresentation>().PresentedActionCount;
        a.Control.SelectTarget(monster);
        a.Control.RequestAttack(2);
        await Wait(() => a.Events.Any(e => e.Kind == EventKind.ActionStarted && e.Entity == a.Net.LocalEntityId &&
                                           (ActionKind)e.Code == ActionKind.Attack),
                   "Approach-to-attack", 15);
        a.Control.ChangeTarget(); // Selection changes must not redirect an attack that already started.
        await Wait(() => monster.Hp < hp, "Authoritative hit");
        await Wait(() => a.Registry.LocalActor.ActionId == 0, "Attack end");
        Check(a.Registry.LocalActor.GetComponent<EntityPresentation>().PresentedActionCount == ownPresentation + 1,
              "Local attack presented more than once");
        Check(observedPlayer.GetComponent<EntityPresentation>().PresentedActionCount == remotePresentation + 1,
              "Remote attack presented more than once");
        Check(b.Registry.TryGet(10001, out var remoteMonster), "B lost monster");
        await Wait(() => Mathf.Abs(remoteMonster.Hp - monster.Hp) < .01f, "HP differs across clients");
        var ownStarts = a.Events
                            .Where(e => e.Kind == EventKind.ActionStarted && e.Entity == a.Net.LocalEntityId &&
                                        (ActionKind)e.Code == ActionKind.Attack)
                            .ToArray();
        Check(ownStarts.Length == ownStarts.Select(e => e.ActionId).Distinct().Count(), "Duplicate action event");
        a.Control.RequestAttack(1);
        a.Control.enabled = false;
        var map = await a.Send(new Request { Kind = RequestKind.ChangeMap, Definition = 2 });
        Check(map.Error == ResultCode.None, "Map change failed");
        await Wait(() => a.Net.Ready && a.Registry.TryGet(11001, out _), "Map 2 snapshot");
        Check(!a.Registry.TryGet(10001, out _), "Old map entities retained");
        net.CloseConnection();
        Object.Destroy(second);
        string result =
            $"PASS: legacy orbit/zoom/follow, camera-relative axis magnitude, cyclic targeting, diagonal movement validation, nearest interaction, two real TCP clients, boundary correction, late interruption, exclusive gathering/cancellation, existing recipes, approach/locked attack, shared HP, action IDs, map snapshots. A events={a.Events.Count}, B events={b.Events.Count}";
        System.IO.File.WriteAllText(".utmp/gameplay/unity-smoke.txt", result);
        return result;
    }
}
