#include "Link.h"

void Link::SendBytes(const std::vector<char> &bytes)
{
    std::lock_guard<std::mutex> guard(mutex);
    size_t sent = 0;
    while (sent < bytes.size() && alive)
    {
        int n = send(socket, bytes.data() + sent, static_cast<int>(bytes.size() - sent), 0);
        if (n <= 0)
        {
            alive = false;
            return;
        }
        sent += n;
    }
}

bool Link::Read(char *buffer, int count)
{
    int offset = 0;
    while (offset < count && alive)
    {
        int n = recv(socket, buffer + offset, count - offset, 0);
        if (n <= 0)
            return false;
        offset += n;
    }
    return offset == count;
}

Link::~Link()
{
    alive = false;
    if (socket != INVALID_SOCKET)
        closesocket(socket);
}
