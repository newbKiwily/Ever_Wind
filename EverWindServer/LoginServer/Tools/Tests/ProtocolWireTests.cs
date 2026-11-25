using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using EverWind.Gameplay;
using UnityEngine;
public static class ProtocolWireTests
{
    const string Folder = "Temp/PacketMigration/fixtures/";
    static void Check(bool value) { if (!value) throw new Exception("Protocol assertion failed"); }
    public static string WriteRequests()
    {
        Directory.CreateDirectory(Folder);
        var request = new Request();
        request.RequestId = 4294967300UL;
        request.ActionId = 4294967301UL;
        request.Epoch = 102;
        request.Revision = 103;
        request.Sequence = 104;
        request.Target = 106;
        request.Definition = 107;
        request.Position = new Vector3(1.25f, -2.5f, 3.75f);
        request.Kind = RequestKind.Join;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1537);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<JoinRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_JOIN_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Move;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1538);
            Check(bytes.Length == 36);
            Check(Marshal.SizeOf<MoveRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_MOVE_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Mode;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1539);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<ModeRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_MODE_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Attack;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1540);
            Check(bytes.Length == 40);
            Check(Marshal.SizeOf<AttackRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_ATTACK_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Gather;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1541);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<GatherRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_GATHER_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Craft;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1542);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<CraftRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_CRAFT_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Cancel;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1543);
            Check(bytes.Length == 28);
            Check(Marshal.SizeOf<CancelRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_CANCEL_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.ChangeMap;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1544);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<ChangeMapRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_CHANGE_MAP_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Revive;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1545);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<ReviveRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_REVIVE_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Probe;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1546);
            Check(bytes.Length == 28);
            Check(Marshal.SizeOf<ProbeRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_PROBE_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.UseItem;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1547);
            Check(bytes.Length == 40);
            Check(Marshal.SizeOf<UseItemRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_USE_ITEM_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Equip;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1548);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<EquipRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_EQUIP_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Unequip;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1549);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<UnequipRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_UNEQUIP_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.ClaimQuest;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1550);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<ClaimQuestRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_CLAIM_QUEST_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Tutorial;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1551);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<TutorialRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_TUTORIAL_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.ResourceRefill;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1552);
            Check(bytes.Length == 28);
            Check(Marshal.SizeOf<ResourceRefillRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_RESOURCE_REFILL_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Save;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1553);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<SaveRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_SAVE_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.Logout;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1554);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<LogoutRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_LOGOUT_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.AttackTargets;
        request.Targets = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(1000, 64));
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1555);
            Check(bytes.Length == 294);
            Check(Marshal.SizeOf<AttackTargetsRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_ATTACK_TARGETS_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.UpgradeStat;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1556);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<UpgradeStatRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_UPGRADE_STAT_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.UpgradeSkill;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1557);
            Check(bytes.Length == 24);
            Check(Marshal.SizeOf<UpgradeSkillRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_UPGRADE_SKILL_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.ResetStats;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1558);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<ResetStatsRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_RESET_STATS_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.ResetSkills;
        request.Targets = Array.Empty<int>();
        {
            var bytes = PacketMethod.BuildGameplayRequest(request);
            Check(BitConverter.ToUInt16(bytes, 0) == bytes.Length);
            Check(BitConverter.ToUInt16(bytes, 2) == 1559);
            Check(bytes.Length == 20);
            Check(Marshal.SizeOf<ResetSkillsRequestPayload>() == bytes.Length - 4);
            File.WriteAllBytes(Folder + "C2S_RESET_SKILLS_REQUEST.bin", bytes);
        }
        request.Kind = RequestKind.AttackTargets;
        request.Targets = Array.Empty<int>();
        File.WriteAllBytes(Folder + "ATTACK_TARGETS_EMPTY.bin", PacketMethod.BuildGameplayRequest(request));
        request.Targets = new int[65];
        bool rejected = false;
        try { PacketMethod.BuildGameplayRequest(request); } catch (InvalidDataException) { rejected = true; }
        Check(rejected);
        request.Targets = new int[1]; request.Kind = RequestKind.Move; rejected = false;
        try { PacketMethod.BuildGameplayRequest(request); } catch (InvalidDataException) { rejected = true; }
        Check(rejected);
        return "PASS: 23 C# request fixtures, zero/max/overflow/wrong-kind target lists";
    }
    public static string ReadEvents()
    {
        MethodInfo read = typeof(PacketMethod).GetMethod("ReadPayload", BindingFlags.NonPublic | BindingFlags.Static);
        int count = 0;
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_ACK.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_ACK);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (AckPayload)read.MakeGenericMethod(typeof(AckPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.ActionId == 4294967301UL);
            Check(payload.Error == ResultCode.Cooldown);
            Check(payload.Code == 117);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Mode == PlayerMode.Combat);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(AckPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_WELCOME.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_WELCOME);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (WelcomePayload)read.MakeGenericMethod(typeof(WelcomePayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.Value == 16.25f);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(WelcomePayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_SPAWN.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_SPAWN);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (SpawnPayload)read.MakeGenericMethod(typeof(SpawnPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(SpawnPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_DESPAWN.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_DESPAWN);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (DespawnPayload)read.MakeGenericMethod(typeof(DespawnPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(DespawnPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_STATE.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_STATE);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (StatePayload)read.MakeGenericMethod(typeof(StatePayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(StatePayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_MOVE_RESULT.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_MOVE_RESULT);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (MoveResultPayload)read.MakeGenericMethod(typeof(MoveResultPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.Error == ResultCode.Cooldown);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(MoveResultPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_ACTION_STARTED.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_ACTION_STARTED);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (ActionStartedPayload)read.MakeGenericMethod(typeof(ActionStartedPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.ActionId == 4294967301UL);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            Check(payload.Error == ResultCode.Cooldown);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(ActionStartedPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_ACTION_ENDED.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_ACTION_ENDED);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (ActionEndedPayload)read.MakeGenericMethod(typeof(ActionEndedPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.ActionId == 4294967301UL);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            Check(payload.Error == ResultCode.Cooldown);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(ActionEndedPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_ACTION_CANCELLED.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_ACTION_CANCELLED);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (ActionCancelledPayload)read.MakeGenericMethod(typeof(ActionCancelledPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.ActionId == 4294967301UL);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            Check(payload.Error == ResultCode.Cooldown);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(ActionCancelledPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_HIT.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_HIT);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (HitPayload)read.MakeGenericMethod(typeof(HitPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.Target == 106);
            Check(payload.ActionId == 4294967301UL);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.Value == 16.25f);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(HitPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_INVENTORY.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_INVENTORY);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (InventoryPayload)read.MakeGenericMethod(typeof(InventoryPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Definition == 107);
            Check(payload.Value == 16.25f);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(InventoryPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_ZONE.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_ZONE);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (ZonePayload)read.MakeGenericMethod(typeof(ZonePayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Target == 106);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(ZonePayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_SNAPSHOT_END.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_SNAPSHOT_END);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (SnapshotEndPayload)read.MakeGenericMethod(typeof(SnapshotEndPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(SnapshotEndPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_STATS.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_STATS);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (StatsPayload)read.MakeGenericMethod(typeof(StatsPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Velocity == new Vector3(1.25f, -2.5f, 3.75f));
            Check(payload.Hp == 11.25f);
            Check(payload.MaxHp == 12.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Definition == 107);
            Check(payload.Mode == PlayerMode.Combat);
            Check(payload.Target == 106);
            Check(payload.Value == 16.25f);
            Check(payload.Duration == 14.25f);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(StatsPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_EQUIPMENT.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_EQUIPMENT);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (EquipmentPayload)read.MakeGenericMethod(typeof(EquipmentPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Code == 117);
            Check(payload.Definition == 107);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(EquipmentPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_QUEST.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_QUEST);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (QuestPayload)read.MakeGenericMethod(typeof(QuestPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Definition == 107);
            Check(payload.Code == 117);
            Check(payload.Value == 16.25f);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(QuestPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_TUTORIAL.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_TUTORIAL);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (TutorialPayload)read.MakeGenericMethod(typeof(TutorialPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Definition == 107);
            Check(payload.Flags == 118);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(TutorialPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_SAVED.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_SAVED);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (SavedPayload)read.MakeGenericMethod(typeof(SavedPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.RequestId == 4294967300UL);
            Check(payload.Code == 117);
            Check(payload.Error == ResultCode.Cooldown);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(SavedPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_GROWTH.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_GROWTH);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (GrowthPayload)read.MakeGenericMethod(typeof(GrowthPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Code == 117);
            Check(payload.Target == 106);
            Check(payload.Definition == 107);
            Check(payload.Value == 16.25f);
            Check(payload.Radius == 13.25f);
            Check(payload.Position == new Vector3(1.25f, -2.5f, 3.75f));
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(GrowthPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        {
            byte[] frame = File.ReadAllBytes(Folder + "S2C_SKILL_RANK.bin");
            Check(BitConverter.ToUInt16(frame, 2) == (ushort)PacketType.S2C_SKILL_RANK);
            Check(BitConverter.ToUInt16(frame, 0) == frame.Length);
            byte[] body = new byte[frame.Length - 4]; Array.Copy(frame, 4, body, 0, body.Length);
            var payload = (SkillRankPayload)read.MakeGenericMethod(typeof(SkillRankPayload)).Invoke(null, new object[] { body });
            Check(payload.Entity == 105);
            Check(payload.Epoch == 102);
            Check(payload.Revision == 103);
            Check(payload.Sequence == 104);
            Check(payload.ServerTime == 12345.5);
            Check(payload.Definition == 107);
            Check(payload.Code == 117);
            Check(payload.Value == 16.25f);
            Check(payload.Duration == 14.25f);
            Check(payload.HitTime == 15.25f);
            bool rejected = false;
            try { read.MakeGenericMethod(typeof(SkillRankPayload)).Invoke(null, new object[] { new byte[body.Length - 1] }); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
            Check(rejected);
            ++count;
        }
        return "PASS: " + count + " native event fixtures decoded in C#, all fields and truncated payloads";
    }
}
