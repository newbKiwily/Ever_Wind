using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EverWind.Gameplay;
using UnityEngine;

public static class PacketDispatchTests
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value) { if (!value) throw new Exception("Dispatch assertion failed"); }

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run in Edit Mode");
        var queue = (Queue<Action>)typeof(UnityMainThreadDispatcher).GetField("executionQueue", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Check(queue.Count == 0);
        var previous = NetworkClient.Current;
        var fixture = new GameObject("PacketDispatchTests") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var transport = fixture.AddComponent<NetworkClient>();
            Check(NetworkClient.Current == previous);
            var pending = (Dictionary<ulong, Action<ServerMessage>>)typeof(NetworkClient).GetField("pending", Private).GetValue(transport);
            byte[] frame = File.ReadAllBytes("Temp/PacketMigration/fixtures/S2C_ACK.bin");
            byte[] body = new byte[frame.Length - 4];
            Array.Copy(frame, 4, body, 0, body.Length);
            ulong requestId = 4294967300UL;
            ServerMessage received = null;
            pending[requestId] = message => received = message;
            PacketMethod.HandlePacket(transport, 0, (ushort)PacketType.S2C_ACK, body);
            Check(received == null && queue.Count == 1);
            queue.Dequeue()();
            Check(received != null && received.Kind == EventKind.Ack && received.RequestId == requestId);
            Check(received.Error == ResultCode.Cooldown && pending.Count == 0);

            received = null;
            pending[requestId] = message => received = message;
            PacketMethod.HandlePacket(transport, 1, (ushort)PacketType.S2C_ACK, body);
            queue.Dequeue()();
            Check(received == null && pending.Count == 1);
            pending.Clear();

            bool rejected = false;
            try { PacketMethod.HandlePacket(transport, 0, 0x501, body); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected && queue.Count == 0);

            var guide = fixture.AddComponent<TutorialGuide>();
            var steps = (Dictionary<TutorialStep, ITutorialStep>)typeof(TutorialGuide).GetField("_stateTable", Private).GetValue(guide);
            steps.Add(TutorialStep.Camera, new CameraStep());
            steps.Add(TutorialStep.Move, new MoveStep());
            steps.Add(TutorialStep.Combat, new CombatStep());
            steps.Add(TutorialStep.Interact, new InteractStep());
            steps.Add(TutorialStep.Craft, new CraftStep());
            steps.Add(TutorialStep.Equip, new EquipStep());
            for (int phase = 1; phase <= 5; ++phase)
            {
                guide.ApplyServerStep(phase, 0);
                var step = steps[(TutorialStep)phase];
                var cleared = step.GetType().GetField("cleared", Private);
                Check(!(bool)cleared.GetValue(step));
                guide.ApplyServerStep(phase, (ushort)(1 << (phase == 1 ? 2 : 1)));
                Check(!(bool)cleared.GetValue(step));
                guide.ApplyServerStep(phase, (ushort)(1 << phase));
                Check((bool)cleared.GetValue(step));
                guide.ApplyServerStep(phase, (ushort)(1 << phase));
                Check((bool)cleared.GetValue(step));
            }
            return "PASS: direct ACK/main-thread callback, stale generation, old ID rejection, five tutorial steps with same-phase/repeated/wrong evidence";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }
}
