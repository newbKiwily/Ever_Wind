#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include "IOCPServer.h"
#include "../LoginServer/SessionManager.h"
#include "Packet.h"
#include "../Logic/PacketMethod.h"
#include <iostream>
#include <stdexcept>

IOCPServer::IOCPServer()
    : listenSocket_(INVALID_SOCKET), completionPort_(nullptr), running_(false), loginLogic_(nullptr), port_(0)
{
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        throw std::runtime_error("Failed to initialize Winsock");
    }
}

IOCPServer::~IOCPServer()
{
    Shutdown();
    WSACleanup();
}

bool IOCPServer::Initialize(uint16_t port, size_t workerCount, PacketMethod* packetMethod, SessionManager* sessionManager)
{
    port_ = port;
    listenSocket_ = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, nullptr, 0, WSA_FLAG_OVERLAPPED);
    if (listenSocket_ == INVALID_SOCKET)
    {
        return false;
    }

    BOOL opt = TRUE;
    setsockopt(listenSocket_, SOL_SOCKET, SO_REUSEADDR, reinterpret_cast<const char*>(&opt), sizeof(opt));

    SOCKADDR_IN addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(port);
    addr.sin_addr.s_addr = htonl(INADDR_ANY);

    if (bind(listenSocket_, reinterpret_cast<SOCKADDR*>(&addr), sizeof(addr)) == SOCKET_ERROR)
    {
        return false;
    }

    if (listen(listenSocket_, SOMAXCONN) == SOCKET_ERROR)
    {
        return false;
    }

    completionPort_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, nullptr, 0, 0);
    if (!completionPort_)
    {
        return false;
    }

    running_.store(true);

    acceptThread_ = std::thread(&IOCPServer::AcceptLoop, this);

    if (workerCount == 0)
    {
        SYSTEM_INFO sysInfo;
        GetSystemInfo(&sysInfo);
        workerCount = sysInfo.dwNumberOfProcessors * 2;
    }

    for (size_t i = 0; i < workerCount; ++i)
    {
        workerThreads_.emplace_back(&IOCPServer::WorkerLoop, this);
    }

    loginLogic_ = packetMethod;
    sessionManager_ = sessionManager;

    return true;
}

void IOCPServer::Shutdown()
{
    if (!running_.exchange(false))
    {
        return;
    }

    if (completionPort_)
    {
        for (size_t i = 0; i < workerThreads_.size(); ++i)
        {
            PostQueuedCompletionStatus(completionPort_, 0, 0, nullptr);
        }
    }

    if (acceptThread_.joinable())
    {
        acceptThread_.join();
    }

    for (auto& thread : workerThreads_)
    {
        if (thread.joinable())
        {
            thread.join();
        }
    }
    workerThreads_.clear();

   

    if (listenSocket_ != INVALID_SOCKET)
    {
        closesocket(listenSocket_);
        listenSocket_ = INVALID_SOCKET;
    }

    if (completionPort_)
    {
        CloseHandle(completionPort_);
        completionPort_ = nullptr;
    }
}

void IOCPServer::AcceptLoop()
{
    while (running_.load())
    {
        SOCKADDR_IN clientAddr;
        int addrLen = sizeof(clientAddr);
        SOCKET clientSocket = accept(listenSocket_, reinterpret_cast<SOCKADDR*>(&clientAddr), &addrLen);
        if (clientSocket == INVALID_SOCKET)
        {
            if (!running_.load())
            {
                break;
            }
            continue;
        }

        HANDLE hResult = CreateIoCompletionPort(
            reinterpret_cast<HANDLE>(clientSocket),
            completionPort_,
            0,
            0
        );

        u_long mode = 1;
        ioctlsocket(clientSocket, FIONBIO, &mode);

        auto session = std::make_shared<Session>(this, clientSocket);
        int result=sessionManager_->AddSession(session);
        session->SetServerUserId(result);
        session->PostRecv();
    }
}

void IOCPServer::WorkerLoop()
{
    while (running_.load())
    {
        DWORD bytesTransferred = 0;
        ULONG_PTR completionKey = 0;
        LPOVERLAPPED overlapped = nullptr;

        BOOL success = GetQueuedCompletionStatus(
            completionPort_,
            &bytesTransferred,
            &completionKey,
            &overlapped,
            INFINITE);

        // 서버 종료용 wake-up 패킷
        if (overlapped == nullptr)
        {
            if (!running_.load())
                break;
            continue;
        }

       
        auto* context = reinterpret_cast<Session::IOContext*>(overlapped);
        if (!context)
            continue;

        std::shared_ptr<Session> session = context->owner;

        // IO 실패 (클라 강제 종료 / 소켓 close 포함)
        if (!success)   
        {
            session->Close();   // 연결만 종료
            delete context;     // IOContext 해제
            continue;
        }

        // 정상 IO 완료
        if (context->operation == Session::IOOperation::Recv)
        {
            session->OnRecvCompleted(context, bytesTransferred);
        }
        else if (context->operation == Session::IOOperation::Send)
        {
            session->OnSendCompleted(context, bytesTransferred);
        }
    }
}


bool IOCPServer::OnPacketReceived(Session* session, const NetPackets::PacketHeader& header, const char* payload, size_t payloadSize)
{
    if (!loginLogic_)
    {
        return false;
    }

    return loginLogic_->HandlePacket(session, header, payload, payloadSize);
}

