#include "MapManager.h"
#include "../GameplayProcessor.h"
#include "RefillSystem.h"
#include "../../Validation/PacketValidator.h"
#include "../../Validation/Validation.h"
#include <algorithm>
#include <chrono>
#include <iostream>
#include <cmath>
#include <filesystem>
#include <fstream>
#include <stdexcept>
namespace EverWind::Gameplay
{
void MapManager::InitializeMaps(const std::string &directory)
{
    sourcePath = (std::filesystem::path(directory) / "map-data").string();

    std::vector<std::filesystem::path> files;

    for (const auto &entry : std::filesystem::directory_iterator(directory))
    {
        if (entry.is_regular_file() && entry.path().extension() == ".txt")
        {
            files.push_back(entry.path());
        }
    }

    std::sort(files.begin(), files.end());

    for (const auto &file : files)
    {
        std::ifstream input(file);

        if (!input)
        {
            throw std::runtime_error("Map file unreadable: " + file.string());
        }

        std::string line;

        while (std::getline(input, line))
        {
            std::istringstream row(line);

            std::string type;

            if (!(row >> type) || type[0] == '#')
            {
                continue;
            }

            if (type == "map" || type == "spawn" || type == "surface" || type == "version" || type == "respawn" || type == "monsterarea")
            {
                ParseMapRow(line);
            }
            else if (type == "monster" || type == "resource" || type == "resourcecfg" || type == "box" || type == "zone")
            {
                objectRows.push_back(line);
            }
            else
            {
                throw std::runtime_error("Unknown map row: " + type);
            }
        }
    }

    if (maps.empty())
    {
        throw std::runtime_error("No server map files: " + directory);
    }

    for (auto &mapPtr : maps)
    {
        auto &map = *mapPtr;

        const int id = map.id;

        if (id < 0 || map.version < 1 || !std::isfinite(map.radius) || map.radius <= 0 || !std::isfinite(map.defaultMonsterRespawnSeconds) || map.defaultMonsterRespawnSeconds < 0 || !std::isfinite(map.monsterRadius) || map.monsterRadius < 0)
        {
            throw std::runtime_error("Invalid map settings");
        }

        map.spawn.y = map.surface.Sample(map.spawn.x, map.spawn.z);
    }
}

void MapManager::InitializeWorldObjects()
{
    for (const auto &line : objectRows)
    {
        ParseMapRow(line);
    }

    for (auto &mapPtr : maps)
    {
        auto &map = *mapPtr;

        for (auto &[rid, resource] : map.resources)
        {
            auto &r = *resource;

            r.capacity = r.stock;

            r.p.y = map.surface.Sample(r.p.x, r.p.z);
        }

        for (auto &[aid, actor] : map.monsters)
        {
            auto &a = *actor;

            a.p.y = map.surface.Sample(a.p.x, a.p.z);

            a.home = a.p;
        }
    }

    for (auto &mapPtr : maps)
    {
        for (auto &[mid, monsterPtr] : mapPtr->monsters)
        {
            auto &map = *mapPtr;
            auto &monster = *monsterPtr;

            if (map.monsterRadius > 0)
            {
                auto point = MonsterSpawn(map, monster.radius, monster.id);

                if (!point)
                {
                    throw std::runtime_error("No valid monster spawn position");
                }

                monster.p = *point;

                monster.home = *point;
            }
        }
    }
}

void MapManager::ParseMapRow(const std::string &line)
{
    std::istringstream s(line);

    std::string type;

    s >> type;

    int id = 0, mapId = 0;

    if (type == "map")
    {
        float r = 0;

        s >> id >> r;

        if (HasMap(id))
        {
            throw std::runtime_error("Duplicate map ID");
        }

        auto map = std::make_unique<Map>();

        map->id = id;

        map->radius = r;

        maps.push_back(std::move(map));
    }
    else if (type == "box")
    {
        Box b{};

        s >> mapId >> b.id >> b.x >> b.z >> b.hx >> b.hz;

        auto level = std::make_shared<StaticLevel>();

        level->id = b.id;

        level->p = {b.x, 0, b.z};

        level->extent = {b.hx, 2, b.hz};

        level->p.y = GetMap(mapId).surface.Sample(b.x, b.z);

        GetMap(mapId).AddActor(level);
    }
    else if (type == "zone")
    {
        Zone z{};

        s >> mapId >> z.id >> z.p.x >> z.p.z >> z.radius;

        GetMap(mapId).zones.push_back(z);
    }
    else if (type == "monster")
    {
        Monster m;

        s >> mapId >> m.id >> m.visual >> m.p.x >> m.p.z >> m.radius >> m.speed >>
            m.hp;
        m.respawnSeconds = GetMap(mapId).defaultMonsterRespawnSeconds;

        if (s.fail())
        {
            throw std::runtime_error("Malformed monster row");
        }

        std::string overrideValue;

        if (s >> overrideValue)
        {
            std::istringstream value(overrideValue);

            if (!(value >> m.respawnSeconds) || !value.eof())
            {
                throw std::runtime_error("Invalid monster respawn value");
            }
        }
        else
        {
            s.clear();
        }

        if (m.respawnSeconds < 0 || m.radius <= 0 || m.hp <= 0 || m.speed < 0)
        {
            throw std::runtime_error("Invalid monster settings");
        }

        m.maxHp = m.hp;

        m.map = mapId;

        m.kind = EntityKind::Monster;

        m.home = m.p;

        GetMap(mapId).AddActor(std::make_shared<Monster>(std::move(m)));
    }
    else if (type == "resource")
    {
        Resource r;

        s >> mapId >> r.id >> r.p.x >> r.p.z >> r.radius >> r.stock >> r.item;

        if (!data.items.count(r.item))
        {
            throw std::runtime_error("Resource references missing DB item");
        }

        GetMap(mapId).AddActor(std::make_shared<Resource>(std::move(r)));
    }
    else if (!ParseMapFeature(type, s))
    {
        throw std::runtime_error("Unknown map row: " + type);
    }

    if (s.fail())
    {
        throw std::runtime_error("Malformed map row: " + line);
    }
}

bool MapManager::ParseMapFeature(const std::string &type, std::istringstream &s)
{
    int id = 0, mapId = 0;

    if (type == "version")
    {
        int value;

        s >> mapId >> value;

        GetMap(mapId).version = value;
    }
    else if (type == "respawn")
    {
        s >> mapId;

        s >> GetMap(mapId).defaultMonsterRespawnSeconds;
    }
    else if (type == "monsterarea")
    {
        s >> mapId;

        auto &map = GetMap(mapId);

        s >> map.monsterCenter.x >> map.monsterCenter.z >> map.monsterRadius;
    }
    else if (type == "spawn")
    {
        s >> mapId;

        auto &m = GetMap(mapId);

        s >> m.spawn.x >> m.spawn.y >> m.spawn.z >> m.center.x >> m.center.z;

        m.surface.flat = m.spawn.y;
    }
    else if (type == "surface")
    {
        s >> mapId;

        auto &h = GetMap(mapId).surface;

        std::string file;

        s >> h.x >> h.z >> h.step >> h.columns >> h.rows >> file;

        if (h.columns < 2 || h.rows < 2 || h.columns > 2048 || h.rows > 2048 || h.step <= 0)
        {
            throw std::runtime_error("Invalid surface");
        }

        h.samples.resize(static_cast<size_t>(h.columns) * h.rows);

        std::ifstream input(std::filesystem::path(sourcePath).parent_path() / file, std::ios::binary);

        if (!input.read(reinterpret_cast<char *>(h.samples.data()), h.samples.size() * sizeof(float)))
        {
            throw std::runtime_error("Surface file missing/truncated");
        }
    }
    else if (type == "resourcecfg")
    {
        s >> mapId >> id;

        auto &r = *GetMap(mapId).resources.at(id);

        s >> r.gatherTime >> r.refillDelay;
    }
    else
    {
        return false;
    }

    return true;
}

std::optional<Vec3> MapManager::MonsterSpawn(Map &map, float radius, int ignoreActor)
{
    std::uniform_real_distribution<float> unit(0, 1);

    for (int attempt = 0; attempt < 256; ++attempt)
    {
        float r = map.monsterRadius * std::sqrt(unit(random)),
              angle = unit(random) * 6.2831853f;
        Vec3 p{map.monsterCenter.x + r * std::cos(angle), 0,
               map.monsterCenter.z + r * std::sin(angle)};
        p.y = map.surface.Sample(p.x, p.z);

        if (Geometry(map, p, p, radius, ignoreActor) == Error::None)
        {
            return p;
        }
    }

    return std::nullopt;
}

bool MapManager::InRange(const Actor &a, const Actor &b, float reach) const
{
    return GetMap(a.map).InRange(a, b, reach);
}
}

namespace EverWind::Gameplay
{
MapManager::MapManager()
    : refill(std::make_unique<RefillSystem>(*this)),
      processor(std::make_unique<GameplayProcessor>(*this, *refill))
{
}

MapManager::~MapManager()
{
    Stop();
}

void MapManager::Initialize(GameDataHub definitions, const std::string &directory)
{
    if (initialized || started || stopped || !maps.empty() || !data.items.empty())
    {
        throw std::logic_error("MapManager already initialized or stopped");
    }

    InitializeGame(std::move(definitions));

    std::cout << "[Initialize] Game data hub and pipeline prepared\n";

    InitializeMaps(directory);

    std::cout << "[Initialize] Maps, terrain and spawn regions prepared\n";

    InitializeWorldObjects();

    std::cout << "[Initialize] Monsters, resources, collision and triggers prepared\n";

    initialized = true;
}

void MapManager::InitializeGame(GameDataHub definitions)
{
    definitions.Validate();

    data = std::move(definitions);
}

const GameDataHub &MapManager::Data() const
{
    return data;
}

bool MapManager::Ready() const
{
    return initialized;
}

bool MapManager::HasMap(int id) const
{
    for (const auto &m : maps)
    {
        if (m->id == id)
        {
            return true;
        }
    }

    return false;
}

Map &MapManager::GetMap(int id)
{
    for (auto &m : maps)
    {
        if (m->id == id)
        {
            return *m;
        }
    }

    throw std::out_of_range("Unknown map");
}

const Map &MapManager::GetMap(int id) const
{
    for (auto &m : maps)
    {
        if (m->id == id)
        {
            return *m;
        }
    }

    throw std::out_of_range("Unknown map");
}

const std::vector<std::unique_ptr<Map>> &MapManager::Maps() const
{
    return maps;
}

std::vector<int> MapManager::MapIds() const
{
    std::vector<int> ids;

    for (auto &m : maps)
    {
        ids.push_back(m->id);
    }

    return ids;
}

std::shared_ptr<Player> MapManager::FindPlayer(int id) const
{
    for (auto &m : maps)
    {
        if (auto p = m->FindPlayer(id))
        {
            return p;
        }
    }

    return {};
}

std::vector<std::pair<int, std::shared_ptr<Player>>>
MapManager::Players() const
{
    std::vector<std::pair<int, std::shared_ptr<Player>>> result;

    for (auto &m : maps)
    {
        for (auto &[id, p] : m->players)
        {
            result.emplace_back(id, m->FindPlayer(id));
        }
    }

    return result;
}

void MapManager::AddPlayer(std::shared_ptr<Player> p, int mapId)
{
    if (FindPlayer(p->id))
    {
        throw std::logic_error("Player already belongs to a map");
    }

    GetMap(mapId).AddActor(std::move(p));
}

void MapManager::TransferPlayer(int id, int destination)
{
    auto &target = GetMap(destination);

    auto p = FindPlayer(id);

    if (!p)
    {
        throw std::logic_error("Player is not connected");
    }

    if (p->map == destination)
    {
        return;
    }

    auto &source = GetMap(p->map);
    // 대상 맵에 먼저 등록하여 실패 시 기존 맵 소속을 유지한다.
    target.AddActor(p);

    source.RemovePlayer(id);
}

void MapManager::RemovePlayer(int id)
{
    for (auto &m : maps)
    {
        m->RemovePlayer(id);
    }
}

Actor *MapManager::Target(int id, int mapId)
{
    auto &m = GetMap(mapId);

    if (auto p = m.FindPlayer(id); p && p->epoch)
    {
        return p.get();
    }

    auto it = m.monsters.find(id);

    return it == m.monsters.end() ? nullptr : it->second;
}

void MapManager::Start()
{
    if (!Ready() || started || stopped)
    {
        throw std::logic_error("MapManager requires complete initialization and one Start");
    }

    thread = std::thread([this]
    {
        Run();
    });

    started = true;
}

bool MapManager::Enqueue(GameWork work, Handler handler)
{
    work.execute = handler ? std::function<Error(Player &, const Request &)>([this, handler](Player &p, const Request &r)
    {
        return ((*processor).*handler)(p, r);
    })
                           : std::function<Error(Player &, const Request &)>{};
    std::lock_guard lock(queueMutex);

    if (!Ready() || stopping || queue.size() >= 4096)
    {
        return false;
    }

    queue.push_back(std::move(work));

    queueReady.notify_one();

    return true;
}

void MapManager::Stop()
{
    stopped = true;

    {
        std::lock_guard lock(queueMutex);

        stopping = true;

        initialized = false;

        queue.clear();
    }

    queueReady.notify_all();

    if (thread.joinable())
    {
        thread.join();
    }

    for (auto &[id, bind] : bindings)
    {
        if (bind)
        {
            bind({});
        }
    }

    bindings.clear();
    connections.clear();

    for (auto &m : maps)
    {
        auto players = m->players;

        for (auto &[id, p] : players)
        {
            m->RemovePlayer(id);
        }
    }
}

void MapManager::Run()
{
    using Clock = std::chrono::steady_clock;

    auto previous = Clock::now();

    auto disconnectClosed = [this]
    {
        for (auto it = connections.begin(); it != connections.end();)
        {
            if (!it->second())
            {
                processor->Disconnect(it->first);

                if (auto b = bindings.find(it->first); b != bindings.end())
                {
                    if (b->second)
                    {
                        b->second({});
                    }

                    bindings.erase(b);
                }

                it = connections.erase(it);
            }
            else
            {
                ++it;
            }
        }
    };

    for (;;)
    {
        std::deque<GameWork> batch;

        {
            std::unique_lock lock(queueMutex);

            queueReady.wait_for(lock, std::chrono::milliseconds(10), [this]
            {
                return stopping || !queue.empty();
            });

            if (stopping)
            {
                break;
            }

            batch.swap(queue);
        }
        // 새 연결의 계정 정보를 읽기 전에 끊어진 연결의 상태를 저장한다.
        disconnectClosed();

        for (auto &w : batch)
        {
            if (!w.alive || !w.alive())
            {
                continue;
            }

            if (!w.authenticated)
            {
                processor->RejectConnection(w);

                continue;
            }

            auto error = Validation::Chain().Then<PacketValidator>().Check(ValidationContext::ForRequest(w.request));

            if (error != Error::None || !w.execute)
            {
                processor->RejectPacket(w, error == Error::None ? Error::BadRequest : error);

                continue;
            }

            if (!connections.count(w.id))
            {
                auto state = w.load ? w.load() : AccountState{};

                if (w.load && !state.loaded)
                {
                    processor->RejectConnection(w);

                    continue;
                }

                processor->Connect(w.id, std::move(w.send), w.moveSpeed, state, std::move(w.save), std::move(w.player));

                connections.emplace(w.id, w.alive);

                if (w.bindPlayer)
                {
                    bindings.emplace(w.id, w.bindPlayer);

                    w.bindPlayer(FindPlayer(w.id));
                }
            }

            processor->Process(w);
        }

        disconnectClosed();

        auto current = Clock::now();

        double dt = std::chrono::duration<double>(current - previous).count();

        previous = current;

        while (dt > 0)
        {
            double step = std::min(dt, .02);

            processor->Advance(step);

            dt -= step;
        }
    }
    // 연결 종료 시 예약을 취소하고 최종 상태를 저장한 뒤 맵에서 제거한다.
    for (const auto &[id, alive] : connections)
    {
        processor->Disconnect(id);
    }
}
}
