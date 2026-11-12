#include "WorldState.h"
#include <algorithm>
#include <cmath>
#include <filesystem>
#include <fstream>
#include <stdexcept>
namespace EverWind::Gameplay
{
WorldState::WorldState(const GameRules &definitions) : rules(definitions)
{
}
void WorldState::InitializeMaps(const std::vector<MapInitialInfo> &config)
{
    if (mapsInitialized || rules.sourcePath.empty())
        throw std::logic_error("Maps require initialized rules and a fresh world");
    for (const auto &line : rules.mapRows)
        ParseMapRow(line);
    if (maps.empty())
        throw std::runtime_error("No maps configured");
    initialMaps = config;
    for (auto &[id, map] : maps)
        map.spawn.y = map.surface.Sample(map.spawn.x, map.spawn.z);
    if (rules.production)
        for (const auto &info : initialMaps)
        {
            auto &map = maps.at(info.mapId);
            map.spawn = {info.playerSpawnX, map.surface.Sample(info.playerSpawnX, info.playerSpawnZ),
                         info.playerSpawnZ};
            map.monsterCenter = {info.enemySpawnX, 0, info.enemySpawnZ};
            map.monsterRadius = info.radius;
        }
    mapsInitialized = true;
}
void WorldState::InitializeWorldObjects()
{
    if (!mapsInitialized || objectsInitialized)
        throw std::logic_error("World objects require maps initialized exactly once");
    for (const auto &line : rules.objectRows)
        ParseMapRow(line);
    for (auto &[id, map] : maps)
    {
        for (auto &[rid, r] : map.resources)
        {
            r.capacity = r.stock;
            r.p.y = map.surface.Sample(r.p.x, r.p.z);
        }
        for (auto &[aid, a] : map.monsters)
        {
            a.p.y = map.surface.Sample(a.p.x, a.p.z);
            a.home = a.p;
        }
    }
    if (!initialMaps.empty())
        ConfigureMaps(initialMaps);
    objectsInitialized = true;
}
void WorldState::ParseMapRow(const std::string &line)
{
    std::istringstream s(line);
    std::string type;
    s >> type;
    int id = 0, mapId = 0;
    if (type == "map")
    {
        float r = 0;
        s >> id >> r;
        maps[id].id = id;
        maps[id].radius = r;
    }
    else if (type == "box")
    {
        Box b{};
        s >> mapId >> b.id >> b.x >> b.z >> b.hx >> b.hz;
        maps.at(mapId).boxes.push_back(b);
    }
    else if (type == "zone")
    {
        Zone z{};
        s >> mapId >> z.id >> z.p.x >> z.p.z >> z.radius;
        maps.at(mapId).zones.push_back(z);
    }
    else if (type == "monster")
    {
        Actor m;
        s >> mapId >> m.id >> m.visual >> m.p.x >> m.p.z >> m.radius >> m.speed >> m.hp;
        m.maxHp = m.hp;
        m.map = mapId;
        m.kind = EntityKind::Monster;
        m.home = m.p;
        maps.at(mapId).monsters.emplace(m.id, m);
    }
    else if (type == "resource")
    {
        Resource r;
        s >> mapId >> r.id >> r.p.x >> r.p.z >> r.radius >> r.stock >> r.item;
        maps.at(mapId).resources.emplace(r.id, r);
    }
    else if (!ParseMapFeature(type, s))
        throw std::runtime_error("Unknown map row: " + type);
    if (s.fail())
        throw std::runtime_error("Malformed map row: " + line);
}
bool WorldState::ParseMapFeature(const std::string &type, std::istringstream &s)
{
    int id = 0, mapId = 0;
    if (type == "spawn")
    {
        s >> mapId;
        auto &m = maps.at(mapId);
        s >> m.spawn.x >> m.spawn.y >> m.spawn.z >> m.center.x >> m.center.z;
        m.surface.flat = m.spawn.y;
    }
    else if (type == "surface")
    {
        s >> mapId;
        auto &h = maps.at(mapId).surface;
        std::string file;
        s >> h.x >> h.z >> h.step >> h.columns >> h.rows >> file;
        if (h.columns < 2 || h.rows < 2 || h.columns > 2048 || h.rows > 2048 || h.step <= 0)
            throw std::runtime_error("Invalid surface");
        h.samples.resize(static_cast<size_t>(h.columns) * h.rows);
        std::ifstream input(std::filesystem::path(rules.sourcePath).parent_path() / file, std::ios::binary);
        if (!input.read(reinterpret_cast<char *>(h.samples.data()), h.samples.size() * sizeof(float)))
            throw std::runtime_error("Surface file missing/truncated");
    }
    else if (type == "resourcecfg")
    {
        s >> mapId >> id;
        auto &r = maps.at(mapId).resources.at(id);
        s >> r.gatherTime >> r.refillDelay;
    }
    else
        return false;
    return true;
}
void WorldState::ConfigureMaps(const std::vector<MapInitialInfo> &config)
{
    if (!rules.production)
        return;
    std::unordered_map<int, Actor> definitions;
    for (auto &[id, map] : maps)
        for (auto &[aid, actor] : map.monsters)
            definitions[actor.visual] = actor;
    for (const auto &info : config)
    {
        auto found = maps.find(info.mapId);
        if (found == maps.end())
            continue;
        auto &map = found->second;
        map.spawn = {info.playerSpawnX, map.surface.Sample(info.playerSpawnX, info.playerSpawnZ),
                     info.playerSpawnZ};
        map.monsterCenter = {info.enemySpawnX, 0, info.enemySpawnZ};
        map.monsterRadius = info.radius;
        map.monsters.clear();
        if (info.enemyIdList.empty())
            continue;
        for (int i = 0; i < info.maxEnemyCount; ++i)
        {
            int visual = info.enemyIdList[i % info.enemyIdList.size()];
            if (!definitions.count(visual))
                throw std::runtime_error("Unknown monster visual definition from original table");
            Actor actor = definitions.at(visual);
            actor.id = 10000 + info.mapId * 1000 + i;
            actor.map = info.mapId;
            auto point = MonsterSpawn(map, actor.radius);
            if (!point)
                throw std::runtime_error("No valid monster spawn point in configured map region");
            actor.p = *point;
            actor.home = actor.p;
            map.monsters.emplace(actor.id, actor);
        }
    }
}
std::optional<Vec3> WorldState::MonsterSpawn(Map &map, float radius)
{
    std::uniform_real_distribution<float> unit(0, 1);
    for (int attempt = 0; attempt < 256; ++attempt)
    {
        float r = map.monsterRadius * std::sqrt(unit(random)), angle = unit(random) * 6.2831853f;
        Vec3 p{map.monsterCenter.x + r * std::cos(angle), 0, map.monsterCenter.z + r * std::sin(angle)};
        p.y = map.surface.Sample(p.x, p.z);
        if (Geometry(map, p, p, radius, true) == Error::None)
            return p;
    }
    return std::nullopt;
}
Actor *WorldState::Target(int id, int map)
{
    if (auto i = players.find(id); i != players.end() && i->second->epoch && i->second->map == map)
        return i->second.get();
    auto &monsters = maps.at(map).monsters;
    auto i = monsters.find(id);
    return i == monsters.end() ? nullptr : &i->second;
}
bool WorldState::InRange(const Actor &a, const Actor &b, float reach) const
{
    return std::abs(a.p.y - b.p.y) < 2 && Distance(a.p, b.p) <= a.radius + b.radius + reach;
}
} // namespace EverWind::Gameplay
