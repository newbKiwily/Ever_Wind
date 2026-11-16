#include "WorldTypes.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay
{
float HeightField::Sample(float px, float pz) const
{
    if (columns < 2 || rows < 2 || samples.size() != static_cast<size_t>(columns * rows))
    {
        return flat;
    }

    float u = std::clamp((px - x) / step, 0.0f, static_cast<float>(columns - 1));
    float v = std::clamp((pz - z) / step, 0.0f, static_cast<float>(rows - 1));

    int ix = std::min(static_cast<int>(u), columns - 2), iz = std::min(static_cast<int>(v), rows - 2);

    u -= ix;

    v -= iz;

    float a = samples[iz * columns + ix] * (1 - u) + samples[iz * columns + ix + 1] * u;
    float b = samples[(iz + 1) * columns + ix] * (1 - u) + samples[(iz + 1) * columns + ix + 1] * u;

    return a * (1 - v) + b * v;
}
}
#include <cstring>
namespace EverWind::Gameplay
{
float Distance(Vec3 a, Vec3 b)
{
    return std::hypot(a.x - b.x, a.z - b.z);
}

Vec3 Add(Vec3 a, Vec3 b)
{
    return {a.x + b.x, a.y + b.y, a.z + b.z};
}

Vec3 Scale(Vec3 a, float v)
{
    return {a.x * v, a.y * v, a.z * v};
}

bool Finite(Vec3 a)
{
    return std::isfinite(a.x) && std::isfinite(a.y) && std::isfinite(a.z);
}
// 캐릭터 반경만큼 확장한 상자와 이동 구간을 비교하여 수평면 충돌을 판정한다.
bool Crosses(Vec3 a, Vec3 b, const Box &box, float radius)
{
    float lo = 0, hi = 1;

    auto axis = [&](float start, float delta, float mn, float mx)
    {
        if (std::abs(delta) < 1e-7f)
        {
            return start > mn && start < mx;
        }

        float t0 = (mn - start) / delta, t1 = (mx - start) / delta;

        if (t0 > t1)
        {
            std::swap(t0, t1);
        }

        lo = std::max(lo, t0);

        hi = std::min(hi, t1);

        return lo < hi;
    };

    return axis(a.x, b.x - a.x, box.x - box.hx - radius, box.x + box.hx + radius) &&
           axis(a.z, b.z - a.z, box.z - box.hz - radius, box.z + box.hz + radius) && hi > 0 && lo < 1;
}

Error Geometry(const Map &map, Vec3 from, Vec3 to, float radius, int ignoreActor, int ignoreTarget)
{
    if (!Finite(to) || std::abs(to.y - map.surface.Sample(to.x, to.z)) > .35f)
    {
        return Error::BadRequest;
    }

    if (std::hypot(to.x - map.center.x, to.z - map.center.z) + radius > map.radius)
    {
        return Error::Collision;
    }

    for (const auto &actor : map.actors)
    {
        if (actor->istrigger || (ignoreActor && actor->id == ignoreActor) || (ignoreTarget && actor->id == ignoreTarget))
        {
            continue;
        }

        Box box{actor->id, actor->p.x, actor->p.z, actor->radius, actor->radius};

        if (auto *level = dynamic_cast<const StaticLevel *>(actor.get()))
        {
            box = level->Footprint();
        }

        if (Crosses(from, to, box, radius))
        {
            return Error::Collision;
        }
    }

    return Error::None;
}

bool Same(const Request &a, const Request &b)
{
    return std::memcmp(&a, &b, sizeof a) == 0;
}
// 요청별 검증은 필요한 검사기를 조합하여 구성한다.
}

namespace EverWind::Gameplay
{
const char *ErrorName(Error error)
{
    static constexpr const char *names[] = {"None", "Unauthenticated", "BadRequest", "WrongWorld", "Stale", "Dead", "Restricted", "OutOfRange", "MissingTarget", "Cooldown", "Busy", "Exhausted", "Ingredients", "Collision", "Speed", "DuplicateConflict", "Expired", "Cancelled"};

    const auto index = static_cast<unsigned>(error);

    return index < sizeof names / sizeof *names ? names[index] : "Unknown";
}
}
