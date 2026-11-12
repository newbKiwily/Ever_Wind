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
void WorldState::InitializeMaps(const std::string &directory)
{
    if (mapsInitialized)
        throw std::logic_error("Maps already initialized");
    sourcePath = (std::filesystem::path(directory) / "map-data").string();
    std::vector<std::filesystem::path> files;
    for (const auto &entry : std::filesystem::directory_iterator(directory))
        if (entry.is_regular_file() && entry.path().extension() == ".txt")
            files.push_back(entry.path());
    std::sort(files.begin(), files.end());
    for (const auto &file : files)
    {
        std::ifstream input(file);
        if (!input)
            throw std::runtime_error("Map file unreadable: " + file.string());
        std::string line;
        while (std::getline(input, line))
        {
            std::istringstream row(line);
            std::string type;
            if (!(row >> type) || type[0] == '#')
                continue;
            if (type == "map" || type == "spawn" || type == "surface" || type == "version" ||
                type == "respawn" || type == "monsterarea")
                ParseMapRow(line);
            else if (type == "monster" || type == "resource" || type == "resourcecfg" || type == "box" ||
                     type == "zone")
                objectRows.push_back(line);
            else
                throw std::runtime_error("Unknown map row: " + type);
        }
    }
    if (maps.empty())
        throw std::runtime_error("No server map files: " + directory);
    for (auto &[id, map] : maps)
    {
        if (id < 0 || map.version < 1 || !std::isfinite(map.radius) || map.radius <= 0 ||
            !std::isfinite(map.defaultMonsterRespawnSeconds) || map.defaultMonsterRespawnSeconds < 0 ||
            !std::isfinite(map.monsterRadius) || map.monsterRadius < 0)
            throw std::runtime_error("Invalid map settings");
        map.spawn.y = map.surface.Sample(map.spawn.x, map.spawn.z);
    }
    mapsInitialized = true;
}
void WorldState::InitializeWorldObjects()
{
    if (!mapsInitialized || objectsInitialized)
        throw std::logic_error("World objects require maps initialized exactly once");
    for (const auto &line : objectRows)
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
    for (auto &[id, map] : maps)
        for (auto &[mid, monster] : map.monsters)
        {
            if (map.monsterRadius > 0)
            {
                auto point = MonsterSpawn(map, monster.radius);
                if (!point)
                    throw std::runtime_error("No valid monster spawn position");
                monster.p = *point;
                monster.home = *point;
            }
        }
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
        if (maps.count(id))
            throw std::runtime_error("Duplicate map ID");
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
        m.respawnSeconds = maps.at(mapId).defaultMonsterRespawnSeconds;
        if (s.fail())
            throw std::runtime_error("Malformed monster row");
        std::string overrideValue;
        if (s >> overrideValue)
        {
            std::istringstream value(overrideValue);
            if (!(value >> m.respawnSeconds) || !value.eof())
                throw std::runtime_error("Invalid monster respawn value");
        }
        else
            s.clear();
        if (m.respawnSeconds < 0 || m.radius <= 0 || m.hp <= 0 || m.speed < 0)
            throw std::runtime_error("Invalid monster settings");
        m.maxHp = m.hp;
        m.map = mapId;
        m.kind = EntityKind::Monster;
        m.home = m.p;
        if (!maps.at(mapId).monsters.emplace(m.id, m).second)
            throw std::runtime_error("Duplicate monster ID");
    }
    else if (type == "resource")
    {
        Resource r;
        s >> mapId >> r.id >> r.p.x >> r.p.z >> r.radius >> r.stock >> r.item;
        if (!rules.items.count(r.item))
            throw std::runtime_error("Resource references missing DB item");
        if (!maps.at(mapId).resources.emplace(r.id, r).second)
            throw std::runtime_error("Duplicate resource ID");
    }
    else if (!ParseMapFeature(type, s))
        throw std::runtime_error("Unknown map row: " + type);
    if (s.fail())
        throw std::runtime_error("Malformed map row: " + line);
}
bool WorldState::ParseMapFeature(const std::string &type, std::istringstream &s)
{
    int id = 0, mapId = 0;
    if (type == "version")
    {
        int value;
        s >> mapId >> value;
        maps.at(mapId).version = value;
    }
    else if (type == "respawn")
    {
        s >> mapId;
        s >> maps.at(mapId).defaultMonsterRespawnSeconds;
    }
    else if (type == "monsterarea")
    {
        s >> mapId;
        auto &map = maps.at(mapId);
        s >> map.monsterCenter.x >> map.monsterCenter.z >> map.monsterRadius;
    }
    else if (type == "spawn")
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
        std::ifstream input(std::filesystem::path(sourcePath).parent_path() / file, std::ios::binary);
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
