#pragma once
#include <atomic>
#include <mutex>
#include <vector>
#include <winsock2.h>

struct Link
{
    SOCKET socket = INVALID_SOCKET;
    std::atomic<bool> alive{true};
    std::mutex mutex;
    void SendBytes(const std::vector<char> &bytes);
    bool Read(char *buffer, int count);
    ~Link();
};
