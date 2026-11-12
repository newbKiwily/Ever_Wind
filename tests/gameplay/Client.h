#pragma once
#include "Gameplay/ServerRuntime.h"
#include "Gameplay/Protocol.h"
#include <algorithm>
#include <cassert>
#include <cmath>
#include <vector>

using namespace EverWind::Gameplay;
struct Client
{
    ServerRuntime &core;
    int id;
    uint64_t next = 0;
    uint32_t epoch = 0, revision = 0, seq = 0;
    Vec3 position{};
    std::vector<Event> events;
    Client(ServerRuntime &c, int id, float moveSpeed = 5.0f,const AccountState& account={},std::function<bool(const AccountState&)> save={});
    Request Make(RequestKind kind);
    Event Send(Request r);
    void Join();
    void MoveTo(Vec3 target);
    size_t Count(EventKind kind);
};
