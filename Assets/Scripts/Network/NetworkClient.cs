using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using EverWind.Gameplay;
using UnityEngine;

[DefaultExecutionOrder(-400)]
public class NetworkClient : SingletonBase<NetworkClient>
{
    private readonly object sendLock = new object();
    private readonly Dictionary<ulong, Action<ServerMessage>> pending = new Dictionary<ulong, Action<ServerMessage>>();
    private readonly Dictionary<ulong, float> deadlines = new Dictionary<ulong, float>();
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiver;
    private volatile bool shuttingDown;
    private int connectionGeneration;
    private ulong nextId, logoutRequest;
    private double receivedAt;

    public static NetworkClient Current { get; private set; }
    public string Host = "127.0.0.1";
    public int Port = 4000;
    public int UserDbId;
    public uint Epoch { get; private set; }
    public int LocalEntityId { get; private set; }
    public bool Ready { get; private set; }
    public string Status { get; private set; } = "Disconnected";
    public double ServerTime { get; private set; }
    public double EstimatedServerTime => ServerTime + (Time.realtimeSinceStartupAsDouble - receivedAt);
    public override bool IsPersistent => true;
    public event Action Disconnected;
    public event Action<int> SignedUp;

    protected override void Awake()
    {
        Priority = -90;
        base.Awake();

        if (Current != null && Current != this)
        {
            return;
        }

        Current = this;
        Application.runInBackground = true;

        if (!UnityMainThreadDispatcher.Exists)
        {
            gameObject.AddComponent<UnityMainThreadDispatcher>();
        }
    }

    public void Login(string user, string password)
    {
        Connect(PacketMethod.BuildLoginRequest(user, password));
    }

    public void Signup(string user, string password)
    {
        Connect(PacketMethod.BuildSignupRequest(user, password));
    }

    private void Connect(byte[] authentication)
    {
        if (receiver != null)
        {
            return;
        }

        ContainerInMap.Current.SetLoading(true);
        Status = "Connecting";
        shuttingDown = false;

        int generation = Interlocked.Increment(ref connectionGeneration);

        receiver = new Thread(() =>
                              {
                                  try
                                  {
                                      using (TcpClient socket = new TcpClient())
                                      {
                                          socket.NoDelay = true;

                                          lock (sendLock)
                                          {
                                              if (!IsCurrent(generation))
                                              {
                                                  return;
                                              }

                                              client = socket;
                                          }

                                          socket.Connect(Host, Port);

                                          NetworkStream connectionStream = socket.GetStream();

                                          lock (sendLock)
                                          {
                                              if (!IsCurrent(generation))
                                              {
                                                  return;
                                              }

                                              stream = connectionStream;
                                          }

                                          SendBytes(authentication);

                                          while (IsCurrent(generation))
                                          {
                                              byte[] header = ReadExact(connectionStream, 4);

                                              ushort length = BitConverter.ToUInt16(header, 0);
                                              ushort id = BitConverter.ToUInt16(header, 2);

                                              if (length < 4 || length > 1024)
                                              {
                                                  throw new InvalidDataException("TCP 프레임 크기 오류");
                                              }

                                              PacketMethod.HandlePacket(this, generation, id, ReadExact(connectionStream, length - 4));
                                          }
                                      }
                                  }
                                  catch (Exception exception)
                                  {
                                      string error = exception.Message;

                                      UnityMainThreadDispatcher.EnqueueWork(() =>
                                                                            {
                                                                                if (IsCurrent(generation))
                                                                                {
                                                                                    Fail(error);
                                                                                }
                                                                            });
                                  }
                              }) { IsBackground = true, Name = "EverWind Receiver" };

        receiver.Start();
    }

    private static byte[] ReadExact(NetworkStream input, int count)
    {
        byte[] buffer = new byte[count];

        int offset = 0;

        while (offset < count)
        {
            int read = input.Read(buffer, offset, count - offset);

            if (read == 0)
            {
                throw new EndOfStreamException("서버 연결이 종료되었습니다.");
            }

            offset += read;
        }

        return buffer;
    }

    public bool IsCurrent(int generation)
    {
        return !shuttingDown && generation == Volatile.Read(ref connectionGeneration);
    }

    private void SendBytes(byte[] buffer)
    {
        lock (sendLock)
        {
            if (stream == null)
            {
                throw new IOException("서버에 연결되어 있지 않습니다.");
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }

    // 과거 호출부의 전송도 현재 서버가 허용하는 계약만 통과시킨다.
    public void Send(byte[] buffer)
    {
        ushort id = BitConverter.ToUInt16(buffer, 2);

        if (!PacketMethod.IsGameplayRequest(id) && id != (ushort)PacketType.C2S_LOGIN_REQ && id != (ushort)PacketType.C2S_SIGNUP_REQ)
        {
            throw new InvalidOperationException("과거 클라이언트 권위 패킷은 전송할 수 없습니다.");
        }

        SendBytes(buffer);
    }

    public ulong Send(Request request, Action<ServerMessage> callback = null)
    {
        if (request.RequestId == 0)
        {
            request.RequestId = ++nextId;
        }

        request.Epoch = Epoch;

        if (callback != null)
        {
            pending[request.RequestId] = callback;
            deadlines[request.RequestId] = Time.realtimeSinceStartup + 5;
        }

        try
        {
            SendBytes(PacketMethod.BuildGameplayRequest(request));
        }
        catch (Exception exception)
        {
            pending.Remove(request.RequestId);
            deadlines.Remove(request.RequestId);
            Fail(exception.Message);
        }

        return request.RequestId;
    }

    public void CompleteLogin(int result, int entity)
    {
        if (result != 0)
        {
            Fail("로그인에 실패했습니다. 계정 정보와 중복 접속을 확인해 주세요.");
            return;
        }

        LocalEntityId = entity;
        UserDbId = entity;
        Status = "Loading world";
        Send(new Request { Kind = RequestKind.Join });
    }

    public void CompleteSignup(int result)
    {
        SignedUp?.Invoke(result);
        CloseConnection();
        UnityEngine.Object.FindFirstObjectByType<LoginUI>()?.SetResult(result == 0 ? "가입되었습니다. 로그인해 주세요." : "가입에 실패했습니다.");
    }

    public bool Accept(ServerMessage message)
    {
        if (message.Kind == EventKind.Ack)
        {
            if (pending.TryGetValue(message.RequestId, out Action<ServerMessage> callback))
            {
                pending.Remove(message.RequestId);
                deadlines.Remove(message.RequestId);
                callback(message);
            }

            return false;
        }

        if (message.Kind == EventKind.Welcome)
        {
            Epoch = message.Epoch;
            Ready = false;
        }

        if (message.Epoch != Epoch)
        {
            return false;
        }

        ServerTime = message.ServerTime;
        receivedAt = Time.realtimeSinceStartupAsDouble;

        if (message.Kind == EventKind.Saved && message.RequestId == logoutRequest)
        {
            logoutRequest = 0;

            if (message.Error == ResultCode.None)
            {
                ReturnToLogin();
                return false;
            }
        }

        return true;
    }

    public void CompleteWorld()
    {
        Ready = true;
        Status = "Connected";
    }

    public bool Logout()
    {
        if (!Ready)
        {
            ReturnToLogin();
            return true;
        }

        logoutRequest = Send(new Request { Kind = RequestKind.Logout });
        return true;
    }

    public void ReturnToLogin()
    {
        CloseConnection();

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LogIn")
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("LogIn");
        }
    }

    public void Fail(string error)
    {
        Status = error;
        CloseConnection();

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "InGame")
        {
            ReturnToLogin();
        }
        else
        {
            UnityEngine.Object.FindFirstObjectByType<LoginUI>()?.SetResult(error);
        }
    }

    private void Update()
    {
        List<ulong> expired = new List<ulong>();

        foreach (KeyValuePair<ulong, float> pair in deadlines)
        {
            if (Time.realtimeSinceStartup > pair.Value)
            {
                expired.Add(pair.Key);
            }
        }

        foreach (ulong id in expired)
        {
            if (!pending.TryGetValue(id, out Action<ServerMessage> callback))
            {
                continue;
            }

            pending.Remove(id);
            deadlines.Remove(id);
            callback(new ServerMessage { Kind = EventKind.Ack, RequestId = id, Error = ResultCode.Expired });
        }
    }

    public void CloseConnection()
    {
        shuttingDown = true;
        Interlocked.Increment(ref connectionGeneration);
        Ready = false;
        Epoch = 0;

        lock (sendLock)
        {
            stream?.Dispose();
            client?.Close();
            stream = null;
            client = null;
        }

        Thread thread = receiver;

        receiver = null;

        if (thread != null && thread != Thread.CurrentThread)
        {
            thread.Join(500);
        }

        pending.Clear();
        deadlines.Clear();
        Disconnected?.Invoke();
        ContainerInMap.Current?.ClearAll();
    }

    protected override void OnDestroy()
    {
        if (Current == this)
        {
            CloseConnection();
            Current = null;
        }

        base.OnDestroy();
    }
}
