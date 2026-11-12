#pragma once
#include <cstddef>
#include <winsock2.h>
#include <windows.h>
#include <memory>

enum class IOOperation
{
    Recv,
    Send,
};

inline constexpr size_t NetworkMaxIoBuffer = 2048;

// Declared here to break the include cycle; only this file owns IOContext.

struct IOContext;

#include "Session.h"

struct IOContext
{
    OVERLAPPED overlapped;
    WSABUF wsaBuf;
    IOOperation operation;
    std::shared_ptr<Session> owner;
    size_t expectedBytes;
    char buffer[NetworkMaxIoBuffer];

    IOContext(std::shared_ptr<Session> o, IOOperation op);
    ~IOContext();
};
