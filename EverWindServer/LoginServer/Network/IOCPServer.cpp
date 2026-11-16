#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include "IOCPServer.h"
#include "../Gameplay/CoreData/Protocol.h"
#include "Packet.h"
#include <cstdlib>
#include <filesystem>
#include <iostream>
#include <stdexcept>
IOCPServer::IOCPServer()
    : listenSocket_(INVALID_SOCKET), completionPort_(nullptr), running_(false), sessionManager_(nullptr),
      port_(0)
{
}

IOCPServer::~IOCPServer()
{
    Shutdown();
}

bool IOCPServer::Initialize(uint16_t port, size_t workerCount, SessionManager *sessions)
{
    if (initialized_ || winsockReady_ || !sessions)
    {
        return false;
    }

    sessionManager_ = sessions;

    port_ = port;

    workerCount_ = workerCount;

    WSADATA data{};

    if (WSAStartup(MAKEWORD(2, 2), &data) != 0)
    {
        return false;
    }

    winsockReady_ = true;

    listenSocket_ = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, nullptr, 0, WSA_FLAG_OVERLAPPED);

    if (listenSocket_ == INVALID_SOCKET)
    {
        Shutdown();

        return false;
    }

    BOOL opt = TRUE;

    setsockopt(listenSocket_, SOL_SOCKET, SO_REUSEADDR, reinterpret_cast<const char *>(&opt), sizeof opt);

    SOCKADDR_IN addr{};

    addr.sin_family = AF_INET;

    addr.sin_port = htons(port);

    addr.sin_addr.s_addr = htonl(INADDR_ANY);

    if (bind(listenSocket_, reinterpret_cast<SOCKADDR *>(&addr), sizeof addr) == SOCKET_ERROR)
    {
        Shutdown();

        return false;
    }

    completionPort_ = CreateIoCompletionPort(INVALID_HANDLE_VALUE, nullptr, 0, 0);

    if (!completionPort_)
    {
        Shutdown();

        return false;
    }

    initialized_ = true;

    return true;
}

bool IOCPServer::Start()
{
    if (!initialized_ || running_.load() || !onPacket_)
    {
        return false;
    }

    if (listen(listenSocket_, SOMAXCONN) == SOCKET_ERROR)
    {
        return false;
    }

    if (workerCount_ == 0)
    {
        SYSTEM_INFO info{};

        GetSystemInfo(&info);

        workerCount_ = info.dwNumberOfProcessors * 2;
    }

    running_ = true;

    accepting_ = true;

    try
    {
        for (size_t i = 0; i < workerCount_; ++i)
        {
            workerThreads_.emplace_back(&IOCPServer::WorkerLoop, this);
        }

        acceptThread_ = std::thread(&IOCPServer::AcceptLoop, this, listenSocket_);
    }
    catch (...)
    {
        Shutdown();

        throw;
    }

    return true;
}

void IOCPServer::StopAccepting()
{
    accepting_ = false;

    if (listenSocket_ != INVALID_SOCKET)
    {
        closesocket(listenSocket_);

        listenSocket_ = INVALID_SOCKET;
    }

    if (acceptThread_.joinable())
    {
        acceptThread_.join();
    }
}

void IOCPServer::Shutdown()
{
    StopAccepting();

    if (sessionManager_)
    {
        sessionManager_->CloseAll();
    }
    // 통신 자원을 파괴하기 전에 취소 완료를 처리하여 세션을 소유한 모든 입출력 문맥을 해제한다.
    {
        std::unique_lock<std::mutex> lock(ioMutex_);

        ioDrained_.wait(lock, [this]
        {
            return outstandingIO_ == 0;
        });
    }

    running_ = false;

    if (completionPort_)
    {
        for (size_t i = 0; i < workerThreads_.size(); ++i)
        {
            PostQueuedCompletionStatus(completionPort_, 0, 0, nullptr);
        }
    }

    for (auto &worker : workerThreads_)
    {
        if (worker.joinable())
        {
            worker.join();
        }
    }

    workerThreads_.clear();

    if (completionPort_)
    {
        CloseHandle(completionPort_);

        completionPort_ = nullptr;
    }

    if (winsockReady_)
    {
        WSACleanup();

        winsockReady_ = false;
    }

    initialized_ = false;
}

void IOCPServer::AcceptLoop(SOCKET listener)
{
    while (accepting_.load())
    {
        SOCKADDR_IN clientAddr;

        int addrLen = sizeof(clientAddr);

        SOCKET clientSocket = accept(listener, reinterpret_cast<SOCKADDR *>(&clientAddr), &addrLen);

        if (clientSocket == INVALID_SOCKET)
        {
            if (!accepting_.load())
            {
                break;
            }

            continue;
        }

        HANDLE hResult =
            CreateIoCompletionPort(reinterpret_cast<HANDLE>(clientSocket), completionPort_, 0, 0);
        if (!hResult)
        {
            closesocket(clientSocket);

            continue;
        }

        u_long mode = 1;

        ioctlsocket(clientSocket, FIONBIO, &mode);

        auto session = std::make_shared<Session>(this, clientSocket);

        int result = sessionManager_->AddSession(session);

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

        BOOL success = GetQueuedCompletionStatus(completionPort_, &bytesTransferred, &completionKey, &overlapped, INFINITE);
        // 서버 종료를 알리는 깨우기 패킷
        if (overlapped == nullptr)
        {
            if (!running_.load())
            {
                break;
            }

            continue;
        }

        auto *context = reinterpret_cast<Session::IOContext *>(overlapped);

        if (!context)
        {
            continue;
        }
        // 이전 입출력을 해제하고 다음 입출력을 요청하는 동안에도 완료 처리 수를 유지한다.
        BeginIO();

        std::shared_ptr<Session> session = context->owner;
        // 입출력 실패로 연결 종료나 소켓 닫힘을 처리한다.
        if (!success)
        {
            session->Close(); // 연결만 종료
            delete context;   // 입출력 문맥 해제
            EndIO();

            continue;
        }
        // 정상 입출력 완료
        if (context->operation == Session::IOOperation::Recv)
        {
            session->OnRecvCompleted(context, bytesTransferred);
        }
        else if (context->operation == Session::IOOperation::Send)
        {
            session->OnSendCompleted(context, bytesTransferred);
        }

        EndIO();
    }
}

bool IOCPServer::OnPacketReceived(Session *session, const NetPackets::PacketHeader &header, const char *payload, size_t payloadSize)
{
    return session && onPacket_ &&
           onPacket_(*session, header, payload, payloadSize);
}

void IOCPServer::SetPacketInput(std::function<bool(Session &, const NetPackets::PacketHeader &, const char *, size_t)> callback)
{
    onPacket_ = std::move(callback);
}

SessionManager *IOCPServer::GetSessionManager()
{
    return sessionManager_;
}

void IOCPServer::BeginIO()
{
    std::lock_guard<std::mutex> lock(ioMutex_);

    ++outstandingIO_;
}

void IOCPServer::EndIO()
{
    std::lock_guard<std::mutex> lock(ioMutex_);

    if (--outstandingIO_ == 0)
    {
        ioDrained_.notify_all();
    }
}
