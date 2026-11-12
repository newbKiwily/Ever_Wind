#include "Client.h"
#include "FeatureTests.h"
#include "ValidationTests.h"
#include <algorithm>
#include <cassert>
#include <cmath>
#include <iostream>
#include <vector>
using namespace EverWind::Gameplay;

void Advance(ServerRuntime &core, double seconds)
{
    while (seconds > .000001)
    {
        double step = std::min(.02, seconds);
        core.Advance(step);
        seconds -= step;
    }
}
// Controlled targets exercise the production hit timer without relying on monster AI timing.
void CombatTimers(const char *rules)
{
    ServerRuntime core; core.InitializeGame(rules); core.InitializeMaps(); core.InitializeWorldObjects();
    Client a(core, 51), b(core, 52);
    a.Join();
    b.Join();
    b.MoveTo({1.8f, 0, -10});
    auto mode = a.Make(RequestKind::Mode);
    mode.definition = 1;
    assert(a.Send(mode).error == Error::None);
    auto attack = a.Make(RequestKind::Attack);
    attack.target = b.id;
    attack.definition = 1;
    assert(a.Send(attack).error == Error::None);
    b.MoveTo({2.6f, 0, -10});
    Advance(core, .3);
    assert(b.Count(EventKind::Hit) == 0); // In range at start, outside at impact.
    Advance(core, .5);
    b.MoveTo({1.8f, 0, -10});
    attack = a.Make(RequestKind::Attack);
    attack.target = b.id;
    attack.definition = 2;
    auto started = a.Send(attack);
    assert(started.error == Error::None);
    auto cancel = a.Make(RequestKind::Cancel);
    cancel.actionId = started.actionId;
    assert(a.Send(cancel).error == Error::None);
    attack = a.Make(RequestKind::Attack);
    attack.target = b.id;
    attack.definition = 2;
    assert(a.Send(attack).error == Error::Cooldown);
    Advance(core, 2.1);
    assert(b.Count(EventKind::Hit) == 0); // Cancelled timer never applies its hit.
    attack = a.Make(RequestKind::Attack);
    attack.target = b.id;
    attack.definition = 2;
    assert(a.Send(attack).error == Error::None);
    uint32_t revision = b.revision;
    Advance(core, 1.3);
    assert(std::any_of(b.events.begin(), b.events.end(),
                       [&](const Event &e) { return e.kind == EventKind::Hit && e.entity == b.id && (e.flags & 1); }));
    assert(b.revision > revision);
    b.MoveTo({1.8f, 0, -10});
    for (int i = 0; i < 7; ++i)
    {
        attack = a.Make(RequestKind::Attack);
        attack.target = b.id;
        attack.definition = 1;
        assert(a.Send(attack).error == Error::None);
        Advance(core, .9);
    }
    auto move = b.Make(RequestKind::Move);
    move.position = b.position;
    move.sequence = ++b.seq;
    assert(b.Send(move).error == Error::Dead);
    assert(b.Send(b.Make(RequestKind::Revive)).error == Error::None);
}
int main(int argc, char **argv)
{
    assert(argc > 1);
    {
        ServerRuntime speedCore; speedCore.InitializeGame(argv[1]); speedCore.InitializeMaps(); speedCore.InitializeWorldObjects();
        Client slow(speedCore, 81, 2), fast(speedCore, 82, 10);
        slow.Join();
        fast.Join();
        for (auto *client : {&slow, &fast})
        {
            auto spawn = std::find_if(client->events.begin(), client->events.end(), [&](const Event &e) {
                return e.kind == EventKind::Spawn && e.entity == client->id;
            });
            assert(spawn != client->events.end() && spawn->duration == (client == &slow ? 2 : 10));
            auto step = client->Make(RequestKind::Move);
            step.sequence = ++client->seq;
            step.position = {.5f, 0, -10};
            assert(client->Send(step).error == Error::None);
        }
        Advance(speedCore, .05);
        for (auto *client : {&slow, &fast})
        {
            auto diagonal = client->Make(RequestKind::Move);
            diagonal.sequence = ++client->seq;
            diagonal.position = {1.1f, 0, -9.4f};
            assert(client->Send(diagonal).error == (client == &slow ? Error::Speed : Error::None));
        }
    }
    CombatTimers(argv[1]);
    FeatureTests();
    ValidationTests();
    ServerRuntime core; core.InitializeGame(argv[1]); core.InitializeMaps(); core.InitializeWorldObjects();
    Client a(core, 1), b(core, 2);
    a.Join();
    b.Join();
    assert(a.Count(EventKind::Spawn) > 5);
    auto invalid = a.Make(RequestKind::Move);
    invalid.sequence = ++a.seq;
    invalid.position = {0, 0, 100};
    assert(a.Send(invalid).error == Error::Collision);
    assert(a.Send(invalid).error == Error::Collision);
    invalid.position = {1, 0, 0};
    assert(a.Send(invalid).error == Error::DuplicateConflict);
    auto mode = a.Make(RequestKind::Mode);
    mode.definition = 1;
    assert(a.Send(mode).error == Error::None);
    auto attack = a.Make(RequestKind::Attack);
    attack.definition = 1;
    attack.target = 10001;
    assert(a.Send(attack).error == Error::OutOfRange);
    mode = a.Make(RequestKind::Mode);
    mode.definition = 0;
    a.Send(mode);
    a.MoveTo({-5, 0, -7});
    b.MoveTo({-5, 0, -7});
    auto gather = a.Make(RequestKind::Gather);
    gather.target = 20001;
    auto begun = a.Send(gather);
    assert(begun.error == Error::None);
    auto action = begun.actionId;
    size_t starts = a.Count(EventKind::ActionStarted);
    a.Send(gather);
    assert(a.Count(EventKind::ActionStarted) == starts);
    auto busy = b.Make(RequestKind::Gather);
    busy.target = 20001;
    assert(b.Send(busy).error == Error::Busy);
    auto cancel = a.Make(RequestKind::Cancel);
    cancel.actionId = action;
    assert(a.Send(cancel).error == Error::None);
    for (int i = 0; i < 120; ++i)
        core.Advance(.02);
    assert(a.Count(EventKind::Inventory) == 0);
    gather = a.Make(RequestKind::Gather);
    gather.target = 20001;
    assert(a.Send(gather).error == Error::None);
    for (int i = 0; i < 110; ++i)
        core.Advance(.02);
    assert(a.Count(EventKind::Inventory) == 1);
    a.Send(gather);
    assert(a.Count(EventKind::Inventory) == 1);
    auto craft = a.Make(RequestKind::Craft);
    craft.definition = 1;
    assert(a.Send(craft).error == Error::Ingredients);
    gather = a.Make(RequestKind::Gather);
    gather.target = 20001;
    assert(a.Send(gather).error == Error::None);
    Advance(core, 2.1);
    craft = a.Make(RequestKind::Craft);
    craft.definition = 2;
    auto crafting = a.Send(craft);
    assert(crafting.error == Error::None);
    cancel = a.Make(RequestKind::Cancel);
    cancel.actionId = crafting.actionId;
    assert(a.Send(cancel).error == Error::None);
    size_t inventoryBefore = a.Count(EventKind::Inventory);
    Advance(core, 3);
    assert(a.Count(EventKind::Inventory) == inventoryBefore);
    craft = a.Make(RequestKind::Craft);
    craft.definition = 2;
    assert(a.Send(craft).error == Error::None);
    Advance(core, 3);
    size_t completedCraft = a.Count(EventKind::Inventory);
    assert(completedCraft == inventoryBefore + 2);
    a.Send(craft);
    assert(a.Count(EventKind::Inventory) == completedCraft);
    auto map = a.Make(RequestKind::ChangeMap);
    map.definition = 2;
    uint32_t oldEpoch = a.epoch;
    assert(a.Send(map).error == Error::None);
    assert(a.epoch != oldEpoch);
    auto old = a.Make(RequestKind::Move);
    old.epoch = oldEpoch;
    old.sequence = ++a.seq;
    old.position = {0, 0, -9};
    assert(a.Send(old).error == Error::WrongWorld);
    map = a.Make(RequestKind::ChangeMap);
    map.definition = 1;
    assert(a.Send(map).error == Error::None);
    auto resource = std::find_if(a.events.rbegin(), a.events.rend(),
                                 [](const Event &e) { return e.entity == 20001 && e.kind == EventKind::State; });
    assert(resource != a.events.rend() && resource->value == 3); // Depletion survives map re-entry.
    core.Disconnect(1);
    core.Disconnect(2);
    std::cout << "PASS: authenticated movement speed/spawn contract, impact-time miss, cancelled attack timer, cooldown, knockback revision, death/revive, "
                 "protocol sizes, movement geometry/revision, idempotency/conflict, attack range, exclusive "
                 "gather, gather/craft cancellation timers, single rewards, recipes, map epochs/re-entry\n";
}
