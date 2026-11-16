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
// 헤더 순환 참조를 해소하기 위한 전방 선언이며 입출력 문맥은 이 파일에서만 정의한다.
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
