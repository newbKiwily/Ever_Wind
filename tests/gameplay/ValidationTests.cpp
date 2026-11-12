#include "ValidationTests.h"
#include "Gameplay/GameplayValidation.h"
#include "Gameplay/Service.h"
#include "Gameplay/Validation/Ingame/ResourceAvailabilityValidator.h"
#include "Gameplay/Validation/Ingame/ResourceExistsValidator.h"
#include "Gameplay/Validation/Ingame/MovementGeometryValidator.h"
#include "Gameplay/Validation/Ingame/ReviveStateValidator.h"
#include "Gameplay/Validation/PacketValidator.h"
#include <type_traits>
#include <atomic>
#include <cassert>
#include <condition_variable>
#include <iostream>
#include <stdexcept>
#include <mutex>
using namespace EverWind::Gameplay;
void ValidationTests()
{
    {
        Map map; map.radius = 10;
        Resource resource; resource.id = 20001; resource.stock = 1; resource.radius = .5f;
        map.resources.emplace(resource.id, resource);
        Actor monster; monster.id = 10001; map.monsters.emplace(monster.id, monster);
        assert(Geometry(map, {-2,0,0}, {2,0,0}, .45f) == Error::None);
        assert(Geometry(map, {-2,0,0}, {2,0,0}, .45f, true) == Error::Collision);
        map.boxes.push_back(Box{1,1,0,.2f,1});
        assert(Geometry(map, {-2,0,0}, {2,0,0}, .45f) == Error::Collision);
        assert(Geometry(map, {0,0,2}, {11,0,2}, .45f) == Error::Collision);
        assert(Geometry(map, {0,0,2}, {0,1,2}, .45f) == Error::BadRequest);
    }
    // Construction does not load rules, create world objects or start a worker.
    ServerRuntime staged;
    bool refusedStage = false;
    try { staged.InitializeMaps(); } catch (const std::logic_error &) { refusedStage = true; }
    assert(refusedStage && !staged.Ready());
    staged.InitializeGame("tests/gameplay/fixtures/Features.rules");
    assert(!staged.HasMap(1) && !staged.Ready());
    staged.InitializeMaps();
    assert(staged.HasMap(1) && !staged.Ready());
    staged.InitializeWorldObjects();
    assert(staged.Ready());
    Service uninitialized;
    refusedStage = false;
    try { uninitialized.Start(); } catch (const std::logic_error &) { refusedStage = true; }
    assert(refusedStage);
    static_assert(std::is_abstract_v<Validator>);
    static_assert(!std::is_abstract_v<PacketValidator>);
    static_assert(std::is_base_of_v<Validator, PacketValidator>);
    static_assert(std::is_base_of_v<Validator, IngameValidator>);
    Request source;
    source.kind = RequestKind::Move;
    source.requestId = 1;
    Request decoded;
    decoded.requestId = 999;
    GameRules decoderWorldRules; decoderWorldRules.Initialize("tests/gameplay/fixtures/Features.rules"); WorldState decoderWorld(decoderWorldRules); decoderWorld.InitializeMaps(); decoderWorld.InitializeWorldObjects();
    RefillSystem decoderRefill(decoderWorld);
    GameplayValidation decoder(decoderWorld, decoderRefill);
    auto bytes = reinterpret_cast<const char *>(&source);
    assert(decoder.Decode(RequestPacket, bytes, sizeof source - 1, true, decoded) == Error::BadRequest);
    assert(decoder.Decode(RequestPacket, nullptr, sizeof source, true, decoded) == Error::BadRequest);
    assert(decoder.Decode(RequestPacket, bytes, sizeof source, false, decoded) == Error::Unauthenticated);
    assert(decoded.requestId == 999);
    assert(decoder.Decode(RequestPacket, bytes, sizeof source, true, decoded) == Error::None);
    assert(Same(decoded, source));
    GameRules worldRules;
    worldRules.Initialize("tests/gameplay/fixtures/Features.rules");
    WorldState world(worldRules);
    assert(world.maps.empty());
    world.InitializeMaps();
    assert(world.maps.at(1).resources.empty());
    world.InitializeWorldObjects();
    assert(world.maps.at(1).resources.count(20001) == 1);
    RefillSystem refill(world);
    GameplayValidation validation(world, refill);
    auto invalid = source;
    invalid.version = ProtocolVersion + 1;
    assert(validation.ValidatePacket(invalid) == Error::BadRequest);
    invalid = source;
    invalid.kind = static_cast<RequestKind>(0);
    assert(validation.ValidatePacket(invalid) == Error::BadRequest);
    invalid = source;
    invalid.requestId = 0;
    assert(validation.ValidatePacket(invalid) == Error::BadRequest);
    Player player;
    player.epoch = 1;
    player.p = {0, 0, -10};
    player.budget = 1;
    auto request = source;
    request.epoch = 1;
    request.sequence = 1;
    request.position = player.p;
    Context context{player, world.maps.at(1), request, 0};
    std::unique_ptr<Validator> packet=std::make_unique<PacketValidator>();
    PacketContext contract;contract.phase=PacketCheckPhase::Contract;contract.request=source;
    packet->Then(std::make_unique<PacketValidator>());
    assert(Validator::InsertBefore(packet,"PacketValidator",std::make_unique<PacketValidator>()));
    assert(packet->Check(contract)==Error::None);
    assert(packet->Check(context)==Error::BadRequest);
    assert(validation.Validate(context) == Error::None);
    player.hp = 0;
    request.epoch = 2;
    assert(validation.Validate(context) == Error::WrongWorld);
    request.epoch = 1;
    assert(validation.Validate(context) == Error::Dead);
    player.hp = 100;
    assert(!validation.InsertBefore(RequestKind::Move, "missing",
                                    std::make_unique<ReviveStateValidator>(world)));
    assert(validation.Validate(context) == Error::None);
    assert(validation.InsertBefore(RequestKind::Move,"MoveSequenceValidator",std::make_unique<MovementGeometryValidator>(world)));
    request.sequence=0;
    assert(validation.Validate(context)==Error::Stale); // Inserted head passed; the original next still runs.
    request.sequence=1;
    assert(validation.Validate(context)==Error::None);
    assert(validation.InsertBefore(RequestKind::Move, "MovementBudgetValidator",
                                   std::make_unique<ReviveStateValidator>(world)));
    for (int i = 0; i < 20; i++)
        assert(validation.Validate(context) == Error::Restricted);
    ResourceExistsValidator resources(world);
    assert(resources.Check(contract)==Error::BadRequest);
    resources.Then(std::make_unique<ResourceAvailabilityValidator>(world));
    request.target = -123;
    assert(resources.Check(context) == Error::MissingTarget); // Does not reach the .at() in the next node.
    bool refused=false;
    try {resources.Then(std::make_unique<ResourceExistsValidator>(world));}catch(const std::logic_error&){refused=true;}
    assert(refused);
    request.target=20001;context.map.resources.at(20001).owner=7;
    assert(resources.Check(context)==Error::Busy); // The original availability node was retained.
    context.map.resources.at(20001).owner=0;
    std::mutex mutex;
    std::condition_variable ready;
    int snapshots = 0, acks = 0, moves = 0;
    bool saved = false;
    std::atomic<bool> alive = true;
    {
        Service service; service.InitializeGame("tests/gameplay/fixtures/Features.rules"); service.InitializeMaps(); service.InitializeWorldObjects(); service.Start();
        auto send = [&](const Event &event) {
            std::lock_guard<std::mutex> lock(mutex);
            if (event.kind == EventKind::SnapshotEnd)
                ++snapshots;
            if (event.kind == EventKind::Ack)
                ++acks;
            if (event.kind == EventKind::MoveResult)
                ++moves;
            ready.notify_all();
        };
        auto living = [&] { return alive.load(); };
        Request join;
        join.requestId = 1;
        // Malformed and unauthenticated frames must close before loading an account.
        std::atomic<int> closed = 0, loads = 0;
        ServiceWork malformed{};
        malformed.id = 902;
        malformed.raw = true;
        malformed.packetId = RequestPacket;
        malformed.authenticated = true;
        malformed.payload.resize(sizeof(Request) - 1);
        malformed.alive = living;
        malformed.send = send;
        malformed.close = [&] { ++closed; ready.notify_all(); };
        malformed.load = [&] { ++loads; return AccountState{}; };
        service.Submit(malformed);
        malformed.payload.resize(sizeof(Request));
        malformed.authenticated = false;
        service.Submit(malformed);
        ServiceWork incoming{};
        incoming.id = 901;
        incoming.raw = true;
        incoming.packetId = RequestPacket;
        incoming.authenticated = true;
        auto begin = reinterpret_cast<const char *>(&join);
        incoming.payload.assign(begin, begin + sizeof join);
        incoming.send = send;
        incoming.alive = living;
        incoming.save = [&](const AccountState &) {
            saved = true;
            return true;
        };
        service.Submit(std::move(incoming));
        {
            std::unique_lock<std::mutex> lock(mutex);
            assert(ready.wait_for(lock, std::chrono::seconds(3), [&] { return snapshots == 1; }));
            assert(closed == 2 && loads == 0);
        }
        Request move;
        move.kind = RequestKind::Move;
        move.requestId = 2;
        move.epoch = 1;
        move.sequence = 1;
        move.position = {0, 0, -10};
        service.Submit(901, move, send, living);
        service.Submit(901, move, send, living);
        {
            std::unique_lock<std::mutex> lock(mutex);
            assert(ready.wait_for(lock, std::chrono::seconds(3), [&] { return acks >= 3; }));
            assert(moves == 1);
        }
        service.Stop();
        assert(saved);
        refusedStage = false;
        try { service.Start(); } catch (const std::logic_error &) { refusedStage = true; }
        assert(refusedStage);
    }
    std::cout << "PASS: typed packet validators, next short-circuit/order, persistent chain insertion, "
                 "serialized processing, replay and shutdown save\n";
}
