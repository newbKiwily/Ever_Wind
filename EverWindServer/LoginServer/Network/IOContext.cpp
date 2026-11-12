#include "IOContext.h"
#include <cstring>
#include <utility>
IOContext::IOContext(std::shared_ptr<Session> o, IOOperation op)
    : operation(op), owner(std::move(o)), expectedBytes(0)
{
    owner->GetServer()->BeginIO();
    memset(&overlapped, 0, sizeof(overlapped));
    wsaBuf.buf = buffer;
    wsaBuf.len = static_cast<ULONG>(NetworkMaxIoBuffer);
}

IOContext::~IOContext()
{
    owner->GetServer()->EndIO();
}
