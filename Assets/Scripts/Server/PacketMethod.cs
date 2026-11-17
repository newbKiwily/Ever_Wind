using System;
using System.IO;
using EverWind.Gameplay;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static unsafe class PacketMethod
{


    private static void CopyStringToFixed(string src, byte *dest, int destSize)
    {
        byte[] srcBytes = Encoding.UTF8.GetBytes(src);
        int copyLen = Math.Min(srcBytes.Length, destSize);
        Marshal.Copy(srcBytes, 0, (IntPtr)dest, copyLen);
        for (int i = copyLen; i < destSize; i++)
            dest[i] = 0;
    }

    public static byte[] BuildLoginRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_LOGIN_REQ)];
        fixed(byte *ptr = buffer)
        {
            PacketHeader *h = (PacketHeader *)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_LOGIN_REQ;

            PKT_LOGIN_REQ *body = (PKT_LOGIN_REQ *)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }





    public static byte[] BuildSignupRequest(string userId, string password)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(PKT_SIGNUP_REQ)];
        fixed(byte *ptr = buffer)
        {
            PacketHeader *h = (PacketHeader *)ptr;
            h->Length = (ushort)buffer.Length;
            h->Id = (ushort)PacketType.C2S_SIGNUP_REQ;

            PKT_SIGNUP_REQ *body = (PKT_SIGNUP_REQ *)(ptr + sizeof(PacketHeader));
            CopyStringToFixed(userId, body->UserID, 32);
            CopyStringToFixed(password, body->Password, 64);
        }
        return buffer;
    }

























    public static byte[] BuildGameplayRequest(Request request)
    {
        byte[] buffer = new byte[sizeof(PacketHeader) + sizeof(RequestPacket)];

        fixed(byte *pointer = buffer)
        {
            *(PacketHeader *)pointer = new PacketHeader { Length = (ushort)buffer.Length, Id = (ushort)PacketType.C2S_GAMEPLAY_REQUEST };
            *(RequestPacket *)(pointer + sizeof(PacketHeader)) = new RequestPacket {
                Version = Protocol.Version, Kind = request.Kind, RequestId = request.RequestId,
                Epoch = request.Epoch, Revision = request.Revision, Sequence = request.Sequence,
                Target = request.Target, Definition = request.Definition, ActionId = request.ActionId,
                X = request.Position.x, Y = request.Position.y, Z = request.Position.z
            };
        }

        return buffer;
    }

    public static void HandlePacket(NetworkClient transport, int generation, ushort id, byte[] payload)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("패킷은 리틀 엔디언을 사용합니다.");
        }

        switch ((PacketType)id)
        {
        case PacketType.S2C_LOGIN_ACK:
            if (payload.Length != sizeof(PKT_LOGIN_ACK))
            {
                throw new InvalidDataException("로그인 응답 크기 오류");
            }

            PKT_LOGIN_ACK login;

            fixed(byte *pointer = payload)
            {
                login = *(PKT_LOGIN_ACK *)pointer;
            }

            UnityMainThreadDispatcher.EnqueueWork(() =>
                                                  {
                                                      if (transport.IsCurrent(generation))
                                                      {
                                                          HandleLoginAck(transport, login);
                                                      }
                                                  });
            break;

        case PacketType.S2C_SIGNUP_ACK:
            if (payload.Length != sizeof(PKT_SIGNUP_ACK))
            {
                throw new InvalidDataException("가입 응답 크기 오류");
            }

            PKT_SIGNUP_ACK signup;

            fixed(byte *pointer = payload)
            {
                signup = *(PKT_SIGNUP_ACK *)pointer;
            }

            UnityMainThreadDispatcher.EnqueueWork(() =>
                                                  {
                                                      if (transport.IsCurrent(generation))
                                                      {
                                                          HandleSignupAck(transport, signup);
                                                      }
                                                  });
            break;

        case PacketType.S2C_GAMEPLAY_EVENT:
            if (payload.Length != sizeof(EventPacket))
            {
                throw new InvalidDataException("게임 이벤트 크기 오류");
            }

            EventPacket packet;

            fixed(byte *pointer = payload)
            {
                packet = *(EventPacket *)pointer;
            }

            if (packet.Version != Protocol.Version || !Enum.IsDefined(typeof(EventKind), packet.Kind))
            {
                throw new InvalidDataException("게임 이벤트 버전 또는 종류 오류");
            }

            ServerMessage message = DecodeGameplayEvent(packet);

            UnityMainThreadDispatcher.EnqueueWork(() =>
                                                  {
                                                      if (transport.IsCurrent(generation))
                                                      {
                                                          HandleGameplayEvent(transport, message);
                                                      }
                                                  });
            break;

        // 인증 시 함께 오는 과거 초기 데이터는 현재 스냅샷으로 대체한다.
        case PacketType._INVENTORYITEM:
        case PacketType._PLAYERSTAT:
        case PacketType.S2C_QUEST_INFO:
            break;

        default:
            throw new InvalidDataException("지원하지 않는 패킷 ID: " + id);
        }
    }

    public static ServerMessage DecodeGameplayEvent(EventPacket packet)
    {
        return new ServerMessage {
            Kind = packet.Kind, Error = packet.Error, Code = packet.Code, Flags = packet.Flags,
            Entity = packet.Entity, Target = packet.Target, Definition = packet.Definition,
            Epoch = packet.Epoch, Revision = packet.Revision, Sequence = packet.Sequence,
            RequestId = packet.RequestId, ActionId = packet.ActionId, ServerTime = packet.ServerTime,
            Position = new Vector3(packet.X, packet.Y, packet.Z), Velocity = new Vector3(packet.VX, packet.VY, packet.VZ),
            Hp = packet.Hp, MaxHp = packet.MaxHp, Radius = packet.Radius, Duration = packet.Duration,
            HitTime = packet.HitTime, Value = packet.Value, Mode = packet.Mode
        };
    }

    public static void HandleLoginAck(NetworkClient transport, PKT_LOGIN_ACK packet)
    {
        transport.CompleteLogin(packet.ResultCode, packet.UserDBID);
    }

    public static void HandleSignupAck(NetworkClient transport, PKT_SIGNUP_ACK packet)
    {
        transport.CompleteSignup(packet.ResultCode);
    }

    public static void HandleGameplayEvent(NetworkClient transport, ServerMessage message)
    {
        if (!transport.Accept(message))
        {
            return;
        }

        switch (message.Kind)
        {
        case EventKind.Welcome:
            HandleWelcomePacket(message);
            break;

        case EventKind.Spawn:
            HandleSpawnPacket(message);
            break;

        case EventKind.SnapshotEnd:
            HandleSnapshotEndPacket(message);
            break;

        default:
            HandleWorldEvent(message);
            break;
        }
    }

    public static void HandleWelcomePacket(ServerMessage message)
    {
        ContainerInMap.Current.Receive(message);
    }

    public static void HandleSpawnPacket(ServerMessage message)
    {
        ContainerInMap.Current.Receive(message);
    }

    public static void HandleSnapshotEndPacket(ServerMessage message)
    {
        ContainerInMap.Current.Receive(message);
    }

    public static void HandleWorldEvent(ServerMessage message)
    {
        ContainerInMap.Current.Receive(message);
    }
}
