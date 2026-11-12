using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace EverWind.Gameplay
{
// Independent scene adapter using the project's existing TCP header and login packet.
// One transport per client; no sockets on entity components, and no legacy singleton activation.
public sealed class NetworkClient : MonoBehaviour
{
    public string Host = "127.0.0.1";
    public int Port = 4000;
    public uint Epoch { get; private set; }
    public int LocalEntityId { get; private set; }
    public string Status { get; private set; } = "Disconnected";
    public bool Ready { get; private set; }
    public event Action<ServerMessage> Message;
    public event Action Disconnected;
    public event Action<int> SignedUp;
    readonly ConcurrentQueue<Action> main = new ConcurrentQueue<Action>();
    readonly Dictionary<ulong, Action<ServerMessage>> pending =
        new Dictionary<ulong, Action<ServerMessage>>();
    readonly Dictionary<ulong, float> deadlines = new Dictionary<ulong, float>();
    readonly object sendLock = new object();
    TcpClient client;
    NetworkStream stream;
    Thread receiver;
    int connectionGeneration;
    ulong nextId;
    bool shuttingDown;
    public double ServerTime { get; private set; }
    double receivedAt;
    public double EstimatedServerTime => ServerTime + (Time.realtimeSinceStartupAsDouble - receivedAt);
    public void Login(string user, string password) => Connect(user, password, false);
    public void Signup(string user, string password) => Connect(user, password, true);
    void Connect(string user, string password, bool signup)
    {
        if (receiver != null)
            return;
        Status = "Connecting";
        shuttingDown = false;
        int generation = ++connectionGeneration;
        receiver =
            new Thread(() =>
                       {
                           try
                           {
                               var socket = new TcpClient();
                               socket.NoDelay = true;
                               lock (sendLock)
                               {
                                   if (shuttingDown || generation != connectionGeneration)
                                   {
                                       socket.Close();
                                       return;
                                   }
                                   client = socket;
                               }
                               socket.Connect(Host, Port);
                               lock (sendLock)
                               {
                                   if (shuttingDown)
                                   {
                                       socket.Close();
                                       return;
                                   }
                                   stream = socket.GetStream();
                               }
                               // Reuse the existing game's authentication encoder; no new
                               // account/authentication system.
                               SendBytes(signup ? global::PacketMethod.BuildSignupRequest(user, password)
                                                : global::PacketMethod.BuildLoginRequest(user, password));
                               while (!shuttingDown && generation == connectionGeneration)
                               {
                                   byte[] header = ReadExact(4);
                                   ushort length = BitConverter.ToUInt16(header, 0),
                                          id = BitConverter.ToUInt16(header, 2);
                                   if (length < 4 || length > 1024)
                                       throw new InvalidDataException("Invalid TCP frame");
                                   byte[] body = ReadExact(length - 4);
                                   if (id == 0x101)
                                   {
                                       if (body.Length != 28)
                                           throw new InvalidDataException("Login ACK length");
                                       int result = BitConverter.ToInt32(body, 0);
                                       int entity = BitConverter.ToInt32(body, 4);
                                       main.Enqueue(() =>
                                                    {
                                                        if (generation != connectionGeneration)
                                                            return;
                                                        if (result != 0)
                                                        {
                                                            Status = "Login rejected: " + result;
                                                            FindFirstObjectByType<LoginUI>()?.SetResult(
                                                                "로그인에 실패했습니다. 계정·비밀번호 또는 " +
                                                                "중복 접속을 확인해 주세요.");
                                                            CloseConnection();
                                                            return;
                                                        }
                                                        LocalEntityId = entity;
                                                        Status = "Loading world";
                                                        Send(new Request { Kind = RequestKind.Join });
                                                    });
                                   }
                                   else if (id == 0x103 && body.Length == 4)
                                   {
                                       int code = BitConverter.ToInt32(body, 0);
                                       main.Enqueue(() =>
                                                    {
                                                        if (generation == connectionGeneration)
                                                        {
                                                            SignedUp?.Invoke(code);
                                                            CloseConnection();
                                                        }
                                                    });
                                   }
                                   else if (id == Protocol.EventPacket)
                                   {
                                       var message = Protocol.Decode(body);
                                       main.Enqueue(() =>
                                                    {
                                                        if (generation == connectionGeneration)
                                                            Receive(message);
                                                    });
                                   }
                                   // Legacy initial stat/quest packets belong to the old scenes, not this new
                                   // gameplay world.
                               }
                           }
                           catch (Exception e)
                           {
                               main.Enqueue(() =>
                                            {
                                                if (generation == connectionGeneration)
                                                {
                                                    Status = e.Message;
                                                    CloseConnection();
                                                }
                                            });
                           }
                       }) { IsBackground = true, Name = "EverWind Gameplay Receiver" };
        receiver.Start();
    }
    byte[] ReadExact(int count)
    {
        var bytes = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int n = stream.Read(bytes, offset, count - offset);
            if (n == 0)
                throw new EndOfStreamException("Server disconnected");
            offset += n;
        }
        return bytes;
    }
    void SendBytes(byte[] bytes)
    {
        lock (sendLock)
        {
            if (stream == null)
                throw new IOException("Not connected");
            stream.Write(bytes, 0, bytes.Length);
        }
    }
    public ulong Send(Request r, Action<ServerMessage> callback = null)
    {
        if (r.RequestId == 0)
            r.RequestId = ++nextId;
        r.Epoch = Epoch;
        if (callback != null)
        {
            pending[r.RequestId] = callback;
            deadlines[r.RequestId] = Time.realtimeSinceStartup + 5;
        }
        try
        {
            SendBytes(Protocol.Encode(r));
        }
        catch (Exception e)
        {
            Status = e.Message;
            pending.Remove(r.RequestId);
            deadlines.Remove(r.RequestId);
            CloseConnection();
        }
        return r.RequestId;
    }
    void Receive(ServerMessage m)
    {
        if (m.Kind == EventKind.Welcome)
        {
            Epoch = m.Epoch;
            Ready = false;
        }
        if (m.Kind == EventKind.Ack)
        {
            if (pending.TryGetValue(m.RequestId, out var callback))
            {
                pending.Remove(m.RequestId);
                deadlines.Remove(m.RequestId);
                callback(m);
            }
            return; // ACK is request bookkeeping, never an entity state transition.
        }
        if (m.Epoch != Epoch)
            return;
        ServerTime = m.ServerTime;
        receivedAt = Time.realtimeSinceStartupAsDouble;
        if (m.Kind == EventKind.SnapshotEnd)
        {
            Ready = true;
            Status = "Connected";
        }
        Message?.Invoke(m);
    }
    void Update()
    {
        int processed = 0;
        while (processed++ < 512 && main.TryDequeue(out var action))
            action();
        var expired = new List<ulong>();
        foreach (var pair in deadlines)
            if (Time.realtimeSinceStartup > pair.Value)
                expired.Add(pair.Key);
        foreach (var id in expired)
        {
            if (!pending.TryGetValue(id, out var cb))
                continue;
            pending.Remove(id);
            deadlines.Remove(id);
            cb(new ServerMessage { Kind = EventKind.Ack, RequestId = id, Error = ResultCode.Expired });
        }
    }
    public void CloseConnection()
    {
        if (Status == "Connected" || Status == "Loading world")
            Status = "Disconnected";
        shuttingDown = true;
        ++connectionGeneration;
        Ready = false;
        Epoch = 0;
        lock (sendLock)
        {
            stream?.Dispose();
            client?.Close();
            stream = null;
            client = null;
        }
        var thread = receiver;
        receiver = null;
        if (thread != null && thread != Thread.CurrentThread)
            thread.Join(500);
        pending.Clear();
        deadlines.Clear();
        Disconnected?.Invoke();
    }
    void OnDestroy()
    {
        CloseConnection();
    }
}
}
