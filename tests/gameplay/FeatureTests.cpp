#include "FeatureTests.h"
#include "Client.h"
#include <iostream>
void FeatureTests()
{
    AccountState saved;
    int saves = 0;
    ServerRuntime core; core.InitializeGame("tests/gameplay/fixtures/Features.rules"); core.InitializeMaps(); core.InitializeWorldObjects();
    AccountState initial;
    initial.loaded = true;
    initial.map = 1;
    initial.tutorial = -1;
    initial.hp = 40;
    initial.inventory = {{1, 2}, {3, 1}, {5, 1}, {11, 1}, {12, 1}};
    Client a(core, 701, 5, initial, [&](const AccountState &state) {
        saved = state;
        ++saves;
        return true;
    });
    a.Join();
    auto submit = [&](RequestKind kind, int definition) {
        auto r = a.Make(kind);
        r.definition = definition;
        return a.Send(r);
    };
    assert(submit(RequestKind::Equip, 11).error == Error::None);
    assert(submit(RequestKind::Equip, 11).error == Error::Ingredients);
    assert(submit(RequestKind::Equip, 5).error == Error::None);
    assert(submit(RequestKind::UseItem, 12).error == Error::None);
    assert(submit(RequestKind::UseItem, 12).error == Error::Ingredients);
    assert(submit(RequestKind::ClaimQuest, 1).error == Error::Restricted);
    auto place = a.Make(RequestKind::UseItem);
    place.definition = 3;
    place.position = {15, 0, 15};
    assert(a.Send(place).error == Error::OutOfRange);
    place = a.Make(RequestKind::UseItem);
    place.definition = 3;
    place.position = a.position;
    assert(a.Send(place).error == Error::None);
    size_t spawns = a.Count(EventKind::Spawn);
    assert(a.Send(place).error == Error::None);
    assert(a.Count(EventKind::Spawn) == spawns);
    auto gather = a.Make(RequestKind::Gather);
    gather.target = 20001;
    assert(a.Send(gather).error == Error::None);
    for (int i = 0; i < 11; i++)
        core.Advance(.02);
    auto resourceState = *std::find_if(a.events.rbegin(), a.events.rend(), [](auto &e) {
        return e.entity == 20001 && e.kind == EventKind::State;
    });
    assert(resourceState.value == 0);
    auto refill = a.Make(RequestKind::ResourceRefill);
    refill.target = 20001;
    refill.sequence = resourceState.sequence;
    assert(a.Send(refill).error == Error::Cooldown);
    for (int i = 0; i < 51; i++)
        core.Advance(.02);
    refill = a.Make(RequestKind::ResourceRefill);
    refill.target = 20001;
    refill.sequence = resourceState.sequence + 99;
    assert(a.Send(refill).error == Error::Stale);
    refill = a.Make(RequestKind::ResourceRefill);
    refill.target = 20001;
    refill.sequence = resourceState.sequence;
    assert(a.Send(refill).error == Error::None);
    assert(submit(RequestKind::Craft, 1).error == Error::None);
    for (int i = 0; i < 11; i++)
        core.Advance(.02);
    auto claim = a.Make(RequestKind::ClaimQuest);
    claim.definition = 1;
    assert(a.Send(claim).error == Error::None);
    auto inventory = a.Count(EventKind::Inventory);
    assert(a.Send(claim).error == Error::None);
    assert(a.Count(EventKind::Inventory) == inventory);
    assert(submit(RequestKind::ClaimQuest, 1).error == Error::Restricted);
    assert(submit(RequestKind::Unequip, 1003).error == Error::None);
    assert(submit(RequestKind::Save, 0).error == Error::None);
    assert(saves > 0);
    assert(saved.inventory.at(11) == 1 && saved.equipment.at(1000) == 5 && saved.hp == 65 &&
           saved.inventory.at(12) == 2 && saved.quests.at(1).claimed);
    core.Disconnect(a.id);
    Client b(core, 702, 5, saved);
    b.Join();
    auto duplicate = b.Make(RequestKind::ClaimQuest);
    duplicate.definition = 1;
    assert(b.Send(duplicate).error == Error::Restricted);
    core.Disconnect(b.id);
    {
        ServerRuntime tutorial; tutorial.InitializeGame("tests/gameplay/fixtures/Tutorial.rules"); tutorial.InitializeMaps(); tutorial.InitializeWorldObjects();
        AccountState state;
        state.loaded = true;
        state.map = 1;
        state.tutorial = 0;
        state.hp = state.maxHp = 10000;
        state.inventory = {{1, 2}, {11, 1}};
        Client player(tutorial, 801, 5, state);
        player.Join();
        auto request = [&](RequestKind kind, int def = 0, int target = 0) {
            auto r = player.Make(kind);
            r.definition = def;
            r.target = target;
            return player.Send(r);
        };
        assert(request(RequestKind::Tutorial, 0).error == Error::Restricted);
        auto move = player.Make(RequestKind::Move);
        move.position = player.position;
        move.sequence = 1;
        assert(player.Send(move).error == Error::Restricted);
        for (int i = 0; i < 251; i++)
            tutorial.Advance(.02);
        assert(request(RequestKind::Tutorial, 0).error == Error::None);
        tutorial.Advance(.02);
        assert(request(RequestKind::Tutorial, 1).error == Error::None);
        assert(request(RequestKind::Tutorial, 2).error == Error::Restricted);
        assert(request(RequestKind::Mode, 1).error == Error::None);
        assert(request(RequestKind::Attack, 3, 10001).error == Error::None);
        for (int i = 0; i < 41; i++)
            tutorial.Advance(.02);
        assert(std::count_if(player.events.begin(), player.events.end(), [](const auto &e) {
                   return e.kind == EventKind::Hit && (e.entity == 10001 || e.entity == 10002);
               }) == 2);
        assert(request(RequestKind::Tutorial, 2).error == Error::None);
        assert(request(RequestKind::Mode, 0).error == Error::None);
        assert(request(RequestKind::Gather, 0, 20001).error == Error::None);
        for (int i = 0; i < 11; i++)
            tutorial.Advance(.02);
        assert(request(RequestKind::Tutorial, 3).error == Error::None);
        assert(request(RequestKind::Craft, 1).error == Error::None);
        for (int i = 0; i < 11; i++)
            tutorial.Advance(.02);
        assert(request(RequestKind::Tutorial, 4).error == Error::None);
        assert(request(RequestKind::Equip, 11).error == Error::None);
        assert(request(RequestKind::Tutorial, 5).error == Error::None);
        assert(player.events.back().error == Error::None);
        assert(std::any_of(player.events.begin(), player.events.end(), [](const auto &e) {
            return e.kind == EventKind::Tutorial && e.definition == -1;
        }));
        tutorial.Disconnect(player.id);
        state.hp = 0;
        state.tutorial = -1;
        Client dead(tutorial, 802, 5, state);
        dead.Join();
        assert(dead.Send(dead.Make(RequestKind::Revive)).error == Error::Cooldown);
        for (int i = 0; i < 751; i++)
            tutorial.Advance(.02);
        assert(dead.Send(dead.Make(RequestKind::Revive)).error == Error::None);
        tutorial.Disconnect(dead.id);
    }
    std::cout << "PASS: server item ownership, equipment replacement/stats, potion/placement, quest reward "
                 "deduplication, client refill timer/generation, save/reconnect\n";
    std::cout << "PASS: six tutorial stages require server evidence, Windmill area impact once per target, "
                 "server revive delay\n";
}
