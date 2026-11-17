#include "GameplayProcessor.h"
#include "../Validation/Ingame/ActionIdleValidator.h"
#include "../Validation/Ingame/AliveRequestValidator.h"
#include "../Validation/Ingame/AttackCooldownValidator.h"
#include "../Validation/Ingame/AttackImpactValidator.h"
#include "../Validation/Ingame/AttackRangeValidator.h"
#include "../Validation/Ingame/AttackTargetValidator.h"
#include "../Validation/Ingame/CancelActionValidator.h"
#include "../Validation/Ingame/CombatModeValidator.h"
#include "../Validation/Ingame/CraftIngredientsValidator.h"
#include "../Validation/Ingame/EquipmentSlotValidator.h"
#include "../Validation/Ingame/EquipmentTypeValidator.h"
#include "../Validation/Ingame/ForcedMovementValidator.h"
#include "../Validation/Ingame/GatherCompletionValidator.h"
#include "../Validation/Ingame/GatherRangeValidator.h"
#include "../Validation/Ingame/ItemExistsValidator.h"
#include "../Validation/Ingame/ItemOwnershipValidator.h"
#include "../Validation/Ingame/ItemUseValidator.h"
#include "../Validation/Ingame/MapExistsValidator.h"
#include "../Validation/Ingame/ModeRequestValidator.h"
#include "../Validation/Ingame/MoveSequenceValidator.h"
#include "../Validation/Ingame/MovementBudgetValidator.h"
#include "../Validation/Ingame/MovementGeometryValidator.h"
#include "../Validation/Ingame/NonCombatModeValidator.h"
#include "../Validation/Ingame/QuestRewardValidator.h"
#include "../Validation/Ingame/RecipeExistsValidator.h"
#include "../Validation/Ingame/ResourceAvailabilityValidator.h"
#include "../Validation/Ingame/ResourceExistsValidator.h"
#include "../Validation/Ingame/ResourceRefillValidator.h"
#include "../Validation/Ingame/ReviveDelayValidator.h"
#include "../Validation/Ingame/ReviveStateValidator.h"
#include "../Validation/Ingame/SkillExistsValidator.h"
#include "../Validation/Ingame/TutorialAttackValidator.h"
#include "../Validation/Ingame/TutorialMapValidator.h"
#include "../Validation/Ingame/TutorialMovementValidator.h"
#include "../Validation/Ingame/TutorialProgressValidator.h"
#include "../Validation/Ingame/WorldEpochValidator.h"
#include "../Validation/PacketValidator.h"
#include "../Validation/Validation.h"
#include <algorithm>
#include <cmath>
#include <iostream>
#include <syncstream>
namespace EverWind::Gameplay
{
GameplayProcessor::GameplayProcessor(MapManager &state, RefillSystem &refillSystem)
    : world(state), refill(refillSystem)
{
}

void GameplayProcessor::Prepare(Player &p)
{
    // 대각선 이동을 포함하여 기존 축 입력의 이동 허용량을 유지한다.
    p.budget =
        std::min(1.0f, p.budget + static_cast<float>(world.now - p.budgetTime) * p.speed * std::sqrt(2.0f));
    p.budgetTime = world.now;
}

void GameplayProcessor::Reject(Player &p, const Request &r, Error error)
{
    if (r.kind == RequestKind::Move && r.epoch == p.epoch && r.revision == p.revision)
    {
        ++p.revision;

        p.sequence = std::max(p.sequence, r.sequence);

        auto e = Base(EventKind::MoveResult, p);

        e.requestId = r.requestId;

        e.error = error;

        SendTo(p, e);
    }
}

Event GameplayProcessor::Base(EventKind kind, const Actor &a) const
{
    Event e;

    e.kind = kind;

    e.entity = a.id;

    e.position = a.p;

    if (dynamic_cast<const IDamageable *>(&a))
    {
        e.velocity = a.velocity;

        e.hp = a.hp;

        e.maxHp = a.maxHp;

        e.mode = a.mode;
    }
    else if (auto level = dynamic_cast<const StaticLevel *>(&a))
    {
        e.velocity = level->extent;
    }

    e.radius = a.radius;

    e.serverTime = world.now;

    e.definition = a.visual;

    if (a.kind == EntityKind::Player)
    {
        const auto &p = static_cast<const Player &>(a);

        e.revision = p.revision;

        e.sequence = p.sequence;
    }

    return e;
}

void GameplayProcessor::SendTo(Player &p, Event e)
{
    e.epoch = p.epoch;

    e.serverTime = world.now;

    if (p.send)
    {
        p.send(e);
    }
}

void GameplayProcessor::Broadcast(int map, Event e)
{
    for (auto &[id, p] : world.Players())
    {
        if (p->epoch && p->map == map)
        {
            SendTo(*p, e);
        }
    }
}

void GameplayProcessor::Inventory(Player &p, int item)
{
    auto e = Base(EventKind::Inventory, p);

    e.definition = item;

    e.value = static_cast<float>(p.inventory[item]);

    SendTo(p, e);
}

void GameplayProcessor::ResourceState(Map &map, Resource &r, Player *only)
{
    Event e;

    e.kind = EventKind::State;

    e.entity = r.id;

    e.code = static_cast<uint16_t>(EntityKind::Resource);

    e.position = r.p;

    e.radius = r.radius;

    e.value = static_cast<float>(r.stock);

    e.target = r.owner;

    e.duration = static_cast<float>(std::max(0.0, r.refillAt - world.now));

    e.sequence = r.generation;

    if (only)
    {
        SendTo(*only, e);
    }
    else
    {
        Broadcast(map.id, e);
    }
}

void GameplayProcessor::Spawn(Actor &a, Player *only)
{
    auto e = Base(EventKind::Spawn, a);

    e.code = static_cast<uint16_t>(a.kind);

    e.duration = dynamic_cast<IDamageable *>(&a) ? a.speed : 0; // 생성 이벤트는 서버 확정 이동 속도를, 행동 이벤트는 행동 지속 시간을 전달한다.
    if (only)
    {
        SendTo(*only, e);
    }
    else
    {
        Broadcast(a.map, e);
    }
}

void GameplayProcessor::Snapshot(Player &p)
{
    auto &m = world.GetMap(p.map);

    Event welcome = Base(EventKind::Welcome, p);

    welcome.value = static_cast<float>(p.map);

    SendTo(p, welcome);

    for (auto &[id, a] : world.Players())
    {
        if (a->epoch && a->map == p.map)
        {
            Spawn(*a, &p);

            if (a->action.id)
            {
                SendTo(p, ActionEvent(EventKind::ActionStarted, *a));
            }
        }
    }

    for (auto &[id, actor] : m.monsters)
    {
        auto &a = *actor;

        Spawn(a, &p);

        if (a.action.id)
        {
            SendTo(p, ActionEvent(EventKind::ActionStarted, a));
        }
    }

    for (auto &[id, resource] : m.resources)
    {
        auto &r = *resource;

        Event e;

        e.kind = EventKind::Spawn;

        e.entity = id;

        e.code = static_cast<uint16_t>(EntityKind::Resource);

        e.position = r.p;

        e.radius = r.radius;

        e.value = static_cast<float>(r.stock);

        e.definition = r.item;

        SendTo(p, e);

        ResourceState(m, r, &p);
    }

    for (const auto &[id, level] : m.levels)
    {
        const auto b = level->Footprint();

        Event e;

        e.kind = EventKind::Spawn;

        e.entity = b.id;

        e.code = static_cast<uint16_t>(EntityKind::Wall);

        e.position = {b.x, 0, b.z};

        e.position.y = m.surface.Sample(b.x, b.z);

        e.velocity = {b.hx, 2, b.hz};

        SendTo(p, e);
    }

    for (const auto &z : m.zones)
    {
        Event e;

        e.kind = EventKind::Spawn;

        e.entity = z.id;

        e.code = static_cast<uint16_t>(EntityKind::Zone);

        e.position = z.p;

        e.radius = z.radius;

        SendTo(p, e);
    }

    Event b;

    b.kind = EventKind::Spawn;

    b.entity = 50000 + p.map;

    b.code = static_cast<uint16_t>(EntityKind::Boundary);

    b.radius = m.radius;

    b.position = m.center;

    SendTo(p, b);

    for (auto [id, n] : p.inventory)
    {
        Inventory(p, id);
    }

    for (auto &[id, a] : m.placements)
    {
        Spawn(*a, &p);
    }

    Stats(p);

    for (auto &[slot, id] : p.equipment)
    {
        Equipment(p, slot);
    }

    for (auto &[id, q] : world.Data().quests)
    {
        QuestState(p, id);
    }

    TutorialState(p);

    auto done = Base(EventKind::SnapshotEnd, p);

    SendTo(p, done);
}

Event GameplayProcessor::ActionEvent(EventKind kind, const Actor &a)
{
    auto e = Base(kind, a);

    e.actionId = a.action.id;

    e.code = static_cast<uint16_t>(a.action.kind);

    e.target = a.action.target;

    e.definition = a.action.definition;

    e.duration = static_cast<float>(a.action.end - a.action.start);
    e.hitTime = static_cast<float>(a.action.impact - a.action.start);
    e.value = static_cast<float>(a.action.start);

    return e;
}

void GameplayProcessor::Cancel(Actor &a, Error reason)
{
    if (!a.action.id)
    {
        return;
    }

    if (a.action.kind == ActionKind::Gather)
    {
        auto &m = world.GetMap(a.map);

        auto i = m.resources.find(a.action.target);

        if (i != m.resources.end() && i->second->owner == a.id)
        {
            i->second->Release(a.id);

            ResourceState(m, *i->second);
        }
    }

    auto e = ActionEvent(EventKind::ActionCancelled, a);

    e.error = reason;

    a.action = {};

    if (a.hp > 0 && a.mode == Mode::Looting)
    {
        a.mode = Mode::NonCombat;
    }

    e.mode = a.mode;

    Broadcast(a.map, e);
}

void GameplayProcessor::Start(Actor &a, ActionKind kind, int target, int definition, float duration, float impact, uint64_t req)
{
    a.action = {world.nextAction++, kind, target,
                definition, world.now, world.now + duration,
                world.now + impact, false};
    auto e = ActionEvent(EventKind::ActionStarted, a);

    e.requestId = req;

    Broadcast(a.map, e);
}

void GameplayProcessor::Zones(Player &p)
{
    for (auto &z : world.GetMap(p.map).zones)
    {
        bool inside = Distance(p.p, z.p) <= z.radius;
        bool was = p.zones.count(z.id) != 0;

        if (inside != was)
        {
            if (inside)
            {
                p.zones.insert(z.id);
            }
            else
            {
                p.zones.erase(z.id);
            }

            auto e = Base(EventKind::Zone, p);

            e.target = z.id;

            e.flags = inside ? 1 : 0;

            Broadcast(p.map, e);
        }
    }
}

void GameplayProcessor::Hit(Actor &attacker, Actor &target, const Skill &skill)
{
    auto *damageable = dynamic_cast<IDamageable *>(&target);

    if (!damageable)
    {
        return;
    }

    int attack = attacker.kind == EntityKind::Player ? static_cast<const Player &>(attacker).AttackPower(world.Data()) : 10;
    int defence = target.kind == EntityKind::Player ? static_cast<const Player &>(target).DefencePower(world.Data()) : 0;

    float damage = skill.ratio >= 0 ? attack * (attacker.kind == EntityKind::Player ? 1 + skill.ratio / 100.0f : 1) * 100.0f / (100 + defence) : skill.damage;

    damageable->ApplyDamage(damage);

    auto e = Base(EventKind::Hit, target);

    e.target = attacker.id;

    e.actionId = attacker.action.id;

    e.value = damage;

    if (target.hp <= 0)
    {
        target.mode = Mode::Dead;

        Cancel(target, Error::Dead);

        target.velocity = {};

        target.pushEnd = 0;

        target.diedAt = world.now;

        if (target.kind == EntityKind::Player)
        {
            auto &p = static_cast<Player &>(target);

            p.reviveAt = world.now + GameDataHub::PlayerReviveDelaySeconds;

            p.dirty = true;
        }

        if (target.kind == EntityKind::Monster && attacker.kind == EntityKind::Player)
        {
            Activity(static_cast<Player &>(attacker), 0, target.visual);
        }
    }
    else if (skill.push > 0)
    {
        Cancel(target);

        Vec3 d{target.p.x - attacker.p.x, 0, target.p.z - attacker.p.z};

        float l = std::max(.001f, Distance(target.p, attacker.p));

        target.push = Scale(d, skill.push / (l * .3f));

        target.pushEnd = world.now + .3;

        target.pushId = world.nextAction++;

        if (target.kind == EntityKind::Player)
        {
            ++static_cast<Player &>(target).revision;
        }

        e.velocity = target.push;

        e.duration = .3f;

        e.flags = 1;
    }

    e.mode = target.mode;

    e.revision = target.kind == EntityKind::Player
                     ? static_cast<Player &>(target).revision
                     : 0;
    if (target.kind == EntityKind::Player)
    {
        static_cast<Player &>(target).dirty = true;
    }

    Broadcast(target.map, e);

    if (target.kind == EntityKind::Player)
    {
        Stats(static_cast<Player &>(target));
    }
}

void GameplayProcessor::UpdateActor(Actor &a, double dt)
{
    auto &m = world.GetMap(a.map);

    if (a.UpdateKnockback(m, world.now, dt) && a.kind == EntityKind::Player)
    {
        auto &p = static_cast<Player &>(a);

        ++p.revision;

        p.budget = 0;

        Broadcast(a.map, Base(EventKind::State, a));
    }

    if (!a.action.id)
    {
        return;
    }

    const auto action = a.action;

    if (action.kind == ActionKind::Attack && !action.hit && world.now >= action.impact)
    {
        a.action.hit = true;

        auto t = world.Target(action.target, a.map);

        const auto &skill =
            a.kind == EntityKind::Player
                ? *static_cast<Player &>(a).FindSkill(action.definition)->definition
                : world.Data().skills.at(action.definition);
        ValidationContext impact;

        impact.actor = &a;

        impact.skill = &skill;

        impact.space = &m;

        if (skill.area > 0 && a.kind == EntityKind::Player)
        {
            for (auto &[id, target] : m.monsters)
            {
                impact.target = target;

                if (Validation::Chain().Then<AttackImpactValidator>(world).Check(impact) == Error::None)
                {
                    Hit(a, *target, skill);
                }
            }
        }
        else
        {
            impact.target = t;

            if (Validation::Chain().Then<AttackImpactValidator>(world).Check(impact) == Error::None)
            {
                Hit(a, *t, skill);
            }
        }
    }

    if (!a.action.id || a.action.id != action.id || world.now < action.end)
    {
        return;
    }

    if (action.kind == ActionKind::Gather)
    {
        auto &p = static_cast<Player &>(a);

        Request completion;

        completion.kind = RequestKind::Gather;

        completion.target = action.target;

        auto context = ValidationContext::ForGame(p, m, completion, world.now);

        if (Validation::Chain().Then<ResourceExistsValidator>().Then<GatherRangeValidator>().Then<GatherCompletionValidator>().Check(context) != Error::None)
        {
            Cancel(a);

            return;
        }

        auto i = m.resources.find(action.target);

        auto &r = *i->second;

        if (!r.Harvest(p.id, world.now))
        {
            Cancel(a);

            return;
        }

        ++p.inventory[r.item];

        Inventory(p, r.item);

        Activity(p, 1, world.Data().items.count(r.item) ? world.Data().items.at(r.item).gatherKey : r.item);

        p.dirty = true;

        ResourceState(m, r);
    }

    if (action.kind == ActionKind::Craft)
    {
        auto &p = static_cast<Player &>(a);

        Request completion;

        completion.kind = RequestKind::Craft;

        completion.definition = action.definition;

        auto context = ValidationContext::ForGame(p, m, completion, world.now);

        if (Validation::Chain().Then<RecipeExistsValidator>(world).Then<CraftIngredientsValidator>(world).Check(context) != Error::None)
        {
            Cancel(a);

            return;
        }

        const auto &r = world.Data().recipes.at(action.definition);

        for (auto [id, n] : r.inputs)
        {
            p.inventory[id] -= n;

            Inventory(p, id);
        }

        p.inventory[r.output] += r.count;

        Inventory(p, r.output);

        Activity(p, 2, world.Data().items.count(r.output) ? world.Data().items.at(r.output).craftKey : r.output);

        p.dirty = true;
    }

    auto e = ActionEvent(EventKind::ActionEnded, a);

    a.action = {};

    if (a.mode == Mode::Looting)
    {
        a.mode = Mode::NonCombat;
    }

    e.mode = a.mode;

    Broadcast(a.map, e);
}

void GameplayProcessor::Advance(double dt)
{
    if (dt <= 0 || dt > .25)
    {
        return;
    }

    world.now += dt;

    for (auto &[id, p] : world.Players())
    {
        if (p->epoch)
        {
            UpdateActor(*p, dt);

            Zones(*p);

            if (auto tutorial = p->FindTutorial(p->tutorial); world.Data().tutorialEnabled && tutorial)
            {
                const auto &step = *tutorial->definition;

                if (step.eventKind == 4 && Distance(p->p, step.position) <= step.radius && !(p->TutorialEvidence() & (1u << p->tutorial)))
                {
                    Activity(*p, 4, 0);
                }
            }

            if (p->dirty && world.now - p->lastSave >= 1)
            {
                Save(*p);
            }
        }
    }

    for (auto &mapPtr : world.Maps())
    {
        for (auto &[id, monster] : mapPtr->monsters)
        {
            auto &map = *mapPtr;

            const int mapId = map.id;

            auto &m = *monster;

            UpdateActor(m, dt);

            if (auto point = refill.MonsterPosition(map, m))
            {
                Broadcast(mapId, Base(EventKind::Despawn, m));

                m.Respawn(*point);

                Spawn(m);
            }

            const auto &skill = world.Data().skills.at(1);

            if (int target = m.UpdateAI(map, skill, world.now, dt))
            {
                Start(m, ActionKind::Attack, target, 1, skill.duration, skill.hit);
            }
        }
    }

    if (world.now >= world.nextSnapshot)
    {
        world.nextSnapshot = world.now + .05;

        for (auto &[id, p] : world.Players())
        {
            if (p->epoch)
            {
                auto e = Base(EventKind::State, *p);

                Broadcast(p->map, e);

                p->velocity = {};
            }
        }

        for (auto &m : world.Maps())
        {
            for (auto &[id, a] : m->monsters)
            {
                Broadcast(m->id, Base(EventKind::State, *a));
            }
        }
    }
}

void GameplayProcessor::Stats(Player &p)
{
    auto e = Base(EventKind::Stats, p);

    e.definition = p.AttackPower(world.Data());

    e.target = p.DefencePower(world.Data());

    e.value = p.speed;

    e.duration = static_cast<float>(std::max(0.0, p.reviveAt - world.now));

    SendTo(p, e);
}

void GameplayProcessor::Equipment(Player &p, int slot)
{
    auto e = Base(EventKind::Equipment, p);

    e.code = static_cast<uint16_t>(slot);

    auto i = p.equipment.find(slot);

    e.definition = i != p.equipment.end() ? i->second : 0;

    Broadcast(p.map, e);
}

void GameplayProcessor::QuestState(Player &p, int id)
{
    auto entry = p.FindQuest(id);

    if (!entry)
    {
        return;
    }

    auto &state = entry->progress;

    const auto &def = *entry->definition;

    state.counts.resize(def.conditions.size());

    state.complete = !def.conditions.empty();

    for (size_t i = 0; i < def.conditions.size(); ++i)
    {
        state.complete &= state.counts[i] >= def.conditions[i].required;
    }

    for (size_t i = 0; i < std::max(size_t(1), def.conditions.size()); ++i)
    {
        auto e = Base(EventKind::Quest, p);

        e.definition = id;

        e.code = static_cast<uint16_t>(i);

        e.value = i < state.counts.size() ? static_cast<float>(state.counts[i]) : 0;

        e.flags = (state.complete ? 1 : 0) | (state.claimed ? 2 : 0);

        SendTo(p, e);
    }
}

void GameplayProcessor::TutorialState(Player &p)
{
    auto e = Base(EventKind::Tutorial, p);

    e.definition = p.tutorial;

    e.flags = static_cast<uint16_t>(p.TutorialEvidence());

    SendTo(p, e);
}

void GameplayProcessor::Progress(Player &p, int kind, int target)
{
    for (auto &entry : p.quests)
    {
        const int id = entry.id;

        const auto &q = *entry.definition;

        auto &state = entry.progress;

        state.counts.resize(q.conditions.size());

        if (state.claimed)
        {
            continue;
        }

        bool changed = false;

        for (size_t i = 0; i < q.conditions.size(); ++i)
        {
            auto &c = q.conditions[i];

            if (c.kind == kind && (c.target == target || c.target == -1))
            {
                state.counts[i] = std::min(c.required, state.counts[i] + 1);

                changed = true;
            }
        }

        if (changed)
        {
            QuestState(p, id);
        }
    }
}

void GameplayProcessor::Activity(Player &p, int kind, int target)
{
    if (kind < 3)
    {
        Progress(p, kind, target);
    }

    if (auto tutorial = p.FindTutorial(p.tutorial))
    {
        const auto &step = *tutorial->definition;

        if (step.eventKind == kind && (step.target == -1 || step.target == target))
        {
            p.FindTutorial(p.tutorial)->evidence |= 1u << p.tutorial;

            TutorialState(p);
        }
    }

    auto e = Base(EventKind::Activity, p);

    e.code = static_cast<uint16_t>(kind);

    e.target = target;

    e.value = 1;

    SendTo(p, e);

    p.dirty = true;
}

bool GameplayProcessor::Save(Player &p, bool force)
{
    if (!p.epoch)
    {
        return true;
    }

    if (!force && !p.dirty)
    {
        return true;
    }

    AccountState state;

    state.loaded = true;

    state.hasPosition = true;

    state.map = p.map;

    state.position = p.p;

    state.hp = p.hp;

    state.maxHp = p.maxHp;

    state.speed = p.speed;

    state.attack = p.baseAttack;

    state.defence = p.baseDefence;

    state.tutorial = p.tutorial;

    state.inventory = p.inventory;

    state.equipment = p.equipment;

    for (const auto &entry : p.quests)
    {
        state.quests.emplace(entry.id, entry.progress);
    }

    bool ok = true;

    try
    {
        if (p.save)
        {
            ok = p.save(state);
        }
    }
    catch (...)
    {
        ok = false;
    }

    p.lastSave = world.now;

    if (ok)
    {
        p.dirty = false;
    }

    return ok;
}

void GameplayProcessor::SaveAll()
{
    for (auto &[id, p] : world.Players())
    {
        if (p->epoch)
        {
            Save(*p, true);
        }
    }
}

void GameplayProcessor::Connect(int id, Send send, float moveSpeed, const AccountState &account, std::function<bool(const AccountState &)> save, std::shared_ptr<Player> player)
{
    if (!world.FindPlayer(id))
    {
        auto p = player ? std::move(player) : std::make_shared<Player>();

        p->id = id;

        if (std::isfinite(moveSpeed) && moveSpeed > 0)
        {
            p->speed = moveSpeed;
        }

        p->send = std::move(send);

        p->account = account;

        p->save = std::move(save);

        p->budgetTime = world.now;

        p->BindDefinitions(world.Data());

        auto ids = world.MapIds();

        int initialMap = account.loaded && world.HasMap(account.map)
                             ? account.map
                             : *std::min_element(ids.begin(), ids.end());
        world.AddPlayer(std::move(p), initialMap);
    }
}

void GameplayProcessor::Disconnect(int id)
{
    auto player = world.FindPlayer(id);

    if (!player)
    {
        return;
    }

    auto &p = *player;

    if (p.epoch)
    {
        Cancel(p);

        Save(p, true);

        Broadcast(p.map, Base(EventKind::Despawn, p));
    }

    world.RemovePlayer(id);
}

void GameplayProcessor::Join(Player &p)
{
    if (!p.epoch)
    {
        p.epoch = 1;

        auto ids = world.MapIds();

        int destination = world.HasMap(1) && !world.Data().tutorialEnabled
                              ? 1
                              : *std::min_element(ids.begin(), ids.end());
        if (p.account.loaded && world.HasMap(p.account.map))
        {
            destination = p.account.map;
        }

        world.TransferPlayer(p.id, destination);

        if (p.account.loaded)
        {
            p.hp = std::clamp(p.account.hp, 0.0f, p.account.maxHp);

            p.maxHp = p.account.maxHp;

            p.speed = p.account.speed;

            p.inventory = p.account.inventory;

            p.equipment = p.account.equipment;

            p.baseAttack = p.account.attack;

            p.baseDefence = p.account.defence;

            p.tutorial = p.account.tutorial;
        }
        else
        {
            p.tutorial = world.Data().tutorialEnabled ? 0 : -1;
        }

        p.p = world.GetMap(p.map).spawn;

        if (p.account.loaded && p.account.hasPosition && Geometry(world.GetMap(p.map), p.account.position, p.account.position, p.radius, p.id) == Error::None)
        {
            p.p = p.account.position;
        }

        p.BeginTutorial(world.now);

        p.mode = p.hp > 0 ? Mode::NonCombat : Mode::Dead;

        if (p.hp <= 0)
        {
            p.reviveAt = world.now + GameDataHub::PlayerReviveDelaySeconds;
        }

        Spawn(p);
    }
}
}

namespace EverWind::Gameplay
{
void GameplayProcessor::RejectConnection(const GameWork &work)
{
    Event event;

    event.kind = EventKind::Ack;

    event.requestId = work.request.requestId;

    event.error = Error::Unauthenticated;

    if (work.send)
    {
        work.send(event);
    }
}
}

namespace EverWind::Gameplay
{
Error GameplayProcessor::HandleJoinRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Join)
    {
        return Error::BadRequest;
    }

    Join(p);

    return Error::None;
}

Error GameplayProcessor::HandleMoveRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Move)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<MoveSequenceValidator>()
                           .Then<ActionIdleValidator>()
                           .Then<MovementGeometryValidator>()
                           .Then<MovementBudgetValidator>()
                           .Then<TutorialMovementValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    float distance = Distance(p.p, r.position);

    p.velocity = Scale({r.position.x - p.p.x, 0, r.position.z - p.p.z}, 20);

    p.budget -= distance;

    p.p = r.position;

    p.sequence = r.sequence;

    Zones(p);

    auto e = Base(EventKind::MoveResult, p);

    e.requestId = r.requestId;

    SendTo(p, e);

    return Error::None;
}

Error GameplayProcessor::HandleModeRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Mode)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error =
        Validation::Chain().Then<ModeRequestValidator>().Check(context);
    if (error != Error::None)
    {
        return error;
    }

    p.mode = static_cast<Mode>(r.definition);

    Broadcast(p.map, Base(EventKind::State, p));

    return Error::None;
}

Error GameplayProcessor::HandleAttackRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Attack)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<SkillExistsValidator>()
                           .Then<CombatModeValidator>()
                           .Then<AttackTargetValidator>(world)
                           .Then<AttackRangeValidator>(world)
                           .Then<AttackCooldownValidator>()
                           .Then<TutorialAttackValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    const auto &s = *p.FindSkill(r.definition)->definition;
    p.FindSkill(r.definition)->cooldownUntil = world.now + s.cooldown;

    Start(p, ActionKind::Attack, r.target, r.definition, s.duration, s.hit, r.requestId);

    return Error::None;
}

Error GameplayProcessor::HandleGatherRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Gather)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<NonCombatModeValidator>()
                           .Then<ResourceExistsValidator>()
                           .Then<GatherRangeValidator>()
                           .Then<ResourceAvailabilityValidator>()
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    auto &map = world.GetMap(p.map);
    auto &resource = *map.resources.at(r.target);

    if (!resource.TryReserve(p.id))
    {
        return Error::Busy;
    }

    p.mode = Mode::Looting;

    Start(p, ActionKind::Gather, r.target, resource.item, resource.gatherTime, resource.gatherTime, r.requestId);

    ResourceState(map, resource);

    return Error::None;
}

Error GameplayProcessor::HandleCraftRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Craft)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<NonCombatModeValidator>()
                           .Then<RecipeExistsValidator>(world)
                           .Then<CraftIngredientsValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    const auto &recipe = world.Data().recipes.at(r.definition);

    p.mode = Mode::Looting;

    Start(p, ActionKind::Craft, 0, r.definition, recipe.duration, recipe.duration, r.requestId);

    return Error::None;
}

Error GameplayProcessor::HandleCancelRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Cancel)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error =
        Validation::Chain().Then<CancelActionValidator>().Check(context);
    if (error != Error::None)
    {
        return error;
    }

    Cancel(p);

    return Error::None;
}

Error GameplayProcessor::HandleChangeMapRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::ChangeMap)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<MapExistsValidator>(world)
                           .Then<TutorialMapValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    Cancel(p);

    auto gone = Base(EventKind::Despawn, p);

    Broadcast(p.map, gone);

    world.TransferPlayer(p.id, r.definition);

    ++p.epoch;

    ++p.revision;

    p.sequence = 0;

    p.p = world.GetMap(p.map).spawn;

    p.velocity = {};

    p.pushEnd = 0;

    p.zones.clear();

    p.mode = p.hp > 0 ? Mode::NonCombat : Mode::Dead;

    Spawn(p);

    return Error::None;
}

Error GameplayProcessor::HandleReviveRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Revive)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ReviveStateValidator>()
                           .Then<ReviveDelayValidator>()
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    p.hp = p.maxHp;

    p.mode = Mode::NonCombat;

    p.p = world.GetMap(p.map).spawn;

    ++p.revision;

    Broadcast(p.map, Base(EventKind::State, p));

    return Error::None;
}

Error GameplayProcessor::HandleProbeRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Probe)
    {
        return Error::BadRequest;
    }

    auto &map = world.GetMap(p.map);

    bool found = false;

    for (auto &[id, level] : map.levels)
    {
        if (id == r.target)
        {
            found = true;

            break;
        }
    }

    if (!found)
    {
        return Error::MissingTarget;
    }

    return Error::None;
}

Error GameplayProcessor::HandleUseItemRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::UseItem)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<ItemExistsValidator>(world)
                           .Then<ItemOwnershipValidator>()
                           .Then<ItemUseValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    auto &item = world.Data().items.at(r.definition);

    --p.inventory[r.definition];

    Inventory(p, r.definition);

    if (item.kind == 2)
    {
        p.hp = std::min(p.maxHp, p.hp + item.heal);

        Broadcast(p.map, Base(EventKind::State, p));

        Stats(p);
    }
    else
    {
        StaticLevel a;

        a.id = world.nextPlacement++;

        a.kind = EntityKind::Placement;

        a.map = p.map;

        a.p = r.position;

        a.visual = r.definition;

        a.radius = 0;

        a.extent = {0, static_cast<float>(r.target) / 100, 0};

        world.GetMap(p.map).AddActor(std::make_shared<StaticLevel>(a));

        Spawn(a);
    }

    return Error::None;
}

Error GameplayProcessor::HandleEquipRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Equip)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<ItemExistsValidator>(world)
                           .Then<ItemOwnershipValidator>()
                           .Then<EquipmentTypeValidator>(world)
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    auto &item = world.Data().items.at(r.definition);

    auto old = p.equipment.find(item.slot);

    if (old != p.equipment.end())
    {
        ++p.inventory[old->second];

        Inventory(p, old->second);
    }

    --p.inventory[r.definition];

    p.equipment[item.slot] = r.definition;

    Inventory(p, r.definition);

    Equipment(p, item.slot);

    Stats(p);

    Activity(p, 3, r.definition);

    return Error::None;
}

Error GameplayProcessor::HandleUnequipRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Unequip)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<EquipmentSlotValidator>()
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    int id = p.equipment.at(r.definition);

    p.equipment.erase(r.definition);

    ++p.inventory[id];

    Inventory(p, id);

    Equipment(p, r.definition);

    Stats(p);

    return Error::None;
}

Error GameplayProcessor::HandleClaimQuestRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::ClaimQuest)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error = Validation::Chain()
                           .Then<ActionIdleValidator>()
                           .Then<QuestRewardValidator>()
                           .Check(context);
    if (error != Error::None)
    {
        return error;
    }

    auto &state = p.FindQuest(r.definition)->progress;

    state.claimed = true;

    for (auto [id, n] : world.Data().quests.at(r.definition).rewards)
    {
        p.inventory[id] += n;

        Inventory(p, id);
    }

    QuestState(p, r.definition);

    return Error::None;
}

Error GameplayProcessor::HandleResourceRefillRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::ResourceRefill)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error =
        Validation::Chain().Then<ResourceRefillValidator>(refill).Check(context);
    if (error != Error::None)
    {
        return error;
    }

    auto &resource = *world.GetMap(p.map).resources.at(r.target);

    if (resource.Refill())
    {
        ResourceState(world.GetMap(p.map), resource);
    }

    return Error::None;
}

Error GameplayProcessor::HandleTutorialRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Tutorial)
    {
        return Error::BadRequest;
    }

    const auto context =
        ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
    const auto error =
        Validation::Chain().Then<TutorialProgressValidator>().Check(context);
    if (error != Error::None)
    {
        return error;
    }

    p.tutorial = p.FindTutorial(p.tutorial)->definition->next;

    p.BeginTutorial(world.now);

    TutorialState(p);

    return Error::None;
}

Error GameplayProcessor::HandleSaveRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Save)
    {
        return Error::BadRequest;
    }

    return SaveAndReply(p, r);
}

Error GameplayProcessor::HandleLogoutRequest(Player &p, const Request &r)
{
    if (r.kind != RequestKind::Logout)
    {
        return Error::BadRequest;
    }

    return SaveAndReply(p, r);
}

Error GameplayProcessor::SaveAndReply(Player &p, const Request &r)
{
    auto e = Base(EventKind::Saved, p);

    e.requestId = r.requestId;

    e.code = static_cast<uint16_t>(r.kind);

    e.error = Save(p, true) ? Error::None : Error::Restricted;

    SendTo(p, e);

    return Error::None;
}
}

namespace
{
void LogMoveError(int id, const EverWind::Gameplay::Request &r, EverWind::Gameplay::Error error)
{
    using namespace EverWind::Gameplay;

    if (r.kind == RequestKind::Move && error != Error::None)
    {
        std::osyncstream(std::cout)
            << "[Move Rejected] connection=" << id << " request=" << r.requestId
            << " sequence=" << r.sequence << " epoch=" << r.epoch
            << " revision=" << r.revision
            << " reason=" << ErrorName(error) << " position=("
            << r.position.x << ',' << r.position.y << ',' << r.position.z << ")\n";
    }
}
}

namespace EverWind::Gameplay
{
void GameplayProcessor::RejectPacket(const GameWork &work, Error error)
{
    LogMoveError(work.id, work.request, error);

    if (auto p = world.FindPlayer(work.id))
    {
        auto ack = Base(EventKind::Ack, *p);

        ack.requestId = work.request.requestId;

        ack.error = error;

        SendTo(*p, ack);
    }
    else
    {
        Event ack;

        ack.kind = EventKind::Ack;

        ack.requestId = work.request.requestId;

        ack.error = error;

        if (work.send)
        {
            work.send(ack);
        }
    }
}

void GameplayProcessor::Process(const GameWork &work)
{
    auto player = world.FindPlayer(work.id);

    if (!player)
    {
        return;
    }

    auto &p = *player;

    const auto &r = work.request;

    auto ack = Base(EventKind::Ack, p);

    ack.requestId = r.requestId;

    if (auto error = Validation::Chain().Then<PacketValidator>().Check(ValidationContext::ForRequest(r)); error != Error::None)
    {
        RejectPacket(work, error);

        return;
    }

    if (auto it = p.cache.find(r.requestId); it != p.cache.end())
    {
        auto replay = it->second.ack;

        if (!Same(it->second.request, r))
        {
            replay.error = Error::DuplicateConflict;
        }

        LogMoveError(work.id, r, replay.error);

        SendTo(p, replay);

        return;
    }

    ack.code = static_cast<uint16_t>(r.kind);

    if (r.requestId <= p.highRequest)
    {
        ack.error = Error::Expired;

        LogMoveError(work.id, r, ack.error);

        SendTo(p, ack);

        return;
    }

    p.highRequest = r.requestId;

    Error error = Error::None;

    if (r.kind != RequestKind::Join)
    {
        if (!p.epoch)
        {
            error = Error::Unauthenticated;
        }
        else
        {
            auto context =
                ValidationContext::ForGame(p, world.GetMap(p.map), r, world.now);
            error = Validation::Chain()
                        .Then<WorldEpochValidator>()
                        .Then<AliveRequestValidator>()
                        .Then<ForcedMovementValidator>()
                        .Check(context);
            Prepare(p);
        }
    }

    if (error == Error::None)
    {
        error = work.execute ? work.execute(p, r) : Error::BadRequest;
    }

    if (error != Error::None && p.epoch)
    {
        Reject(p, r, error);
    }

    if (error == Error::None && r.kind != RequestKind::Join)
    {
        p.dirty = true;
    }

    LogMoveError(work.id, r, error);

    ack.error = error;

    ack.position = p.p;

    ack.revision = p.revision;

    ack.sequence = p.sequence;

    ack.mode = p.mode;

    ack.actionId = p.action.id;

    p.cache.emplace(r.requestId, CachedRequest{r, ack});

    p.order.push_back(r.requestId);

    if (p.order.size() > 512)
    {
        p.cache.erase(p.order.front());

        p.order.pop_front();
    }

    SendTo(p, ack);

    if (error == Error::None && (r.kind == RequestKind::Join || r.kind == RequestKind::ChangeMap))
    {
        Snapshot(p);
    }
}
}
