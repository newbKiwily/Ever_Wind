#pragma once
#include "../Map/Entity/Monster.h"
#include "../Map/Entity/Player.h"
#include "../Map/Entity/Resource.h"
#include "../Map/Entity/StaticLevel.h"
#include "GameplayData.h"
#include "../Map/Map.h"
#include <optional>
namespace EverWind::Gameplay
{
struct Context
{
    const Player &player;

    const Map &map;

    const Request &request;

    double now;
};

struct ValidationContext
{
    uint16_t id = 0;

    const char *payload = nullptr;

    size_t size = 0;

    bool authenticated = false;

    const Request *request = nullptr;

    std::optional<Context> game;

    const Actor *actor = nullptr, *target = nullptr;

    const Skill *skill = nullptr;

    const Map *space = nullptr;

    static ValidationContext ForRequest(const Request &request)
    {
        ValidationContext c;

        c.request = &request;

        return c;
    }

    static ValidationContext ForGame(const Player &p, const Map &m, const Request &r, double now)
    {
        auto c = ForRequest(r);

        c.game.emplace(Context{p, m, r, now});

        return c;
    }
};

struct GameWork
{
    int id = 0;

    Request request;

    Send send;

    std::function<bool()> alive;

    float moveSpeed = 5.0f;

    std::function<bool(const AccountState &)> save;

    std::function<AccountState()> load;

    bool authenticated = false;

    std::function<void(std::shared_ptr<Player>)> bindPlayer;

    std::shared_ptr<Player> player;
    // 큐에 넣을 때 작업을 만들고 플레이어와 요청 참조는 실행 중에만 사용한다.
    std::function<Error(Player &, const Request &)> execute;
};
}

namespace EverWind::Gameplay
{
const char *ErrorName(Error error);

float Distance(Vec3 a, Vec3 b);

Vec3 Add(Vec3 a, Vec3 b);

Vec3 Scale(Vec3 a, float v);

bool Finite(Vec3 a);

bool Crosses(Vec3 a, Vec3 b, const Box &box, float radius);
// 트리거 개체는 통과하며 판정 대상 자신은 충돌 검사에서 제외한다.
Error Geometry(const Map &map, Vec3 from, Vec3 to, float radius, int ignoreActor = 0, int ignoreTarget = 0);

bool Same(const Request &a, const Request &b);
}
