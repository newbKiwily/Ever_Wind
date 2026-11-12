#include "GameplayProcessor.h"
#include <algorithm>
#include <cmath>
namespace EverWind::Gameplay {
GameplayProcessor::GameplayProcessor(WorldState &state, RefillSystem &refillSystem) : world(state), refill(refillSystem) {}
void GameplayProcessor::Prepare(Player &p) {
  // Preserve the original unnormalized axis movement budget, including diagonal
  // speed.
  p.budget =
      std::min(1.0f, p.budget + static_cast<float>(world.now - p.budgetTime) *
                                    p.speed * std::sqrt(2.0f));
  p.budgetTime = world.now;
}
void GameplayProcessor::Reject(Player &p, const Request &r, Error error) {
  if (r.kind == RequestKind::Move && r.epoch == p.epoch &&
      r.revision == p.revision) {
    ++p.revision;
    p.sequence = std::max(p.sequence, r.sequence);
    auto e = Base(EventKind::MoveResult, p);
    e.requestId = r.requestId;
    e.error = error;
    SendTo(p, e);
  }
}
Event GameplayProcessor::Base(EventKind kind, const Actor &a) const {
  Event e;
  e.kind = kind;
  e.entity = a.id;
  e.position = a.p;
  e.velocity = a.velocity;
  e.hp = a.hp;
  e.maxHp = a.maxHp;
  e.radius = a.radius;
  e.mode = a.mode;
  e.serverTime = world.now;
  e.definition = a.visual;
  if (a.kind == EntityKind::Player) {
    const auto &p = static_cast<const Player &>(a);
    e.revision = p.revision;
    e.sequence = p.sequence;
  }
  return e;
}
void GameplayProcessor::SendTo(Player &p, Event e) {
  e.epoch = p.epoch;
  e.serverTime = world.now;
  if (output)
    output(p.send, e);
}
void GameplayProcessor::Broadcast(int map, Event e) {
  for (auto &[id, p] : world.players)
    if (p->epoch && p->map == map)
      SendTo(*p, e);
}
void GameplayProcessor::Inventory(Player &p, int item) {
  auto e = Base(EventKind::Inventory, p);
  e.definition = item;
  e.value = static_cast<float>(p.inventory[item]);
  SendTo(p, e);
}
void GameplayProcessor::ResourceState(Map &map, Resource &r, Player *only) {
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
    SendTo(*only, e);
  else
    Broadcast(map.id, e);
}
void GameplayProcessor::Spawn(Actor &a, Player *only) {
  auto e = Base(EventKind::Spawn, a);
  e.code = static_cast<uint16_t>(a.kind);
  e.duration = a.speed; // Spawn carries the authoritative movement speed;
                        // action events carry action duration.
  if (only)
    SendTo(*only, e);
  else
    Broadcast(a.map, e);
}
void GameplayProcessor::Snapshot(Player &p) {
  auto &m = world.maps.at(p.map);
  Event welcome = Base(EventKind::Welcome, p);
  welcome.value = static_cast<float>(p.map);
  SendTo(p, welcome);
  for (auto &[id, a] : world.players)
    if (a->epoch && a->map == p.map) {
      Spawn(*a, &p);
      if (a->action.id)
        SendTo(p, ActionEvent(EventKind::ActionStarted, *a));
    }
  for (auto &[id, a] : m.monsters) {
    Spawn(a, &p);
    if (a.action.id)
      SendTo(p, ActionEvent(EventKind::ActionStarted, a));
  }
  for (auto &[id, r] : m.resources) {
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
  for (const auto &b : m.boxes) {
    Event e;
    e.kind = EventKind::Spawn;
    e.entity = b.id;
    e.code = static_cast<uint16_t>(EntityKind::Wall);
    e.position = {b.x, 0, b.z};
    e.position.y = m.surface.Sample(b.x, b.z);
    e.velocity = {b.hx, 2, b.hz};
    SendTo(p, e);
  }
  for (const auto &z : m.zones) {
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
    Inventory(p, id);
  for (auto &[id, a] : m.placements)
    Spawn(a, &p);
  Stats(p);
  for (auto &[slot, id] : p.equipment)
    Equipment(p, slot);
  for (auto &[id, q] : world.rules.quests)
    QuestState(p, id);
  TutorialState(p);
  auto done = Base(EventKind::SnapshotEnd, p);
  SendTo(p, done);
}
Event GameplayProcessor::ActionEvent(EventKind kind, const Actor &a) {
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
void GameplayProcessor::Cancel(Actor &a, Error reason) {
  if (!a.action.id)
    return;
  if (a.action.kind == ActionKind::Gather) {
    auto &m = world.maps.at(a.map);
    auto i = m.resources.find(a.action.target);
    if (i != m.resources.end() && i->second.owner == a.id) {
      i->second.owner = 0;
      ResourceState(m, i->second);
    }
  }
  auto e = ActionEvent(EventKind::ActionCancelled, a);
  e.error = reason;
  a.action = {};
  if (a.hp > 0 && a.mode == Mode::Looting)
    a.mode = Mode::NonCombat;
  e.mode = a.mode;
  Broadcast(a.map, e);
}
void GameplayProcessor::Start(Actor &a, ActionKind kind, int target,
                              int definition, float duration, float impact,
                              uint64_t req) {
  a.action = {world.nextAction++, kind,      target,
              definition,         world.now, world.now + duration,
              world.now + impact, false};
  auto e = ActionEvent(EventKind::ActionStarted, a);
  e.requestId = req;
  Broadcast(a.map, e);
}
void GameplayProcessor::Zones(Player &p) {
  for (auto &z : world.maps.at(p.map).zones) {
    bool inside = Distance(p.p, z.p) <= z.radius;
    bool was = p.zones.count(z.id) != 0;
    if (inside != was) {
      if (inside)
        p.zones.insert(z.id);
      else
        p.zones.erase(z.id);
      auto e = Base(EventKind::Zone, p);
      e.target = z.id;
      e.flags = inside ? 1 : 0;
      Broadcast(p.map, e);
    }
  }
}
void GameplayProcessor::Hit(Actor &attacker, Actor &target,
                            const Skill &skill) {
  float damage = skill.ratio >= 0 ? AttackPower(attacker) *
                                        (attacker.kind == EntityKind::Player
                                             ? 1 + skill.ratio / 100.0f
                                             : 1) *
                                        100.0f / (100 + DefencePower(target))
                                  : skill.damage;
  target.hp = std::max(0.0f, target.hp - damage);
  auto e = Base(EventKind::Hit, target);
  e.target = attacker.id;
  e.actionId = attacker.action.id;
  e.value = damage;
  if (target.hp <= 0) {
    target.mode = Mode::Dead;
    Cancel(target, Error::Dead);
    target.velocity = {};
    target.pushEnd = 0;
    target.diedAt = world.now;
    if (target.kind == EntityKind::Player) {
      auto &p = static_cast<Player &>(target);
      p.reviveAt = world.now + world.rules.reviveDelay;
      p.dirty = true;
    }
    if (target.kind == EntityKind::Monster &&
        attacker.kind == EntityKind::Player)
      Activity(static_cast<Player &>(attacker), 0, target.visual);
  } else if (skill.push > 0) {
    Cancel(target);
    Vec3 d{target.p.x - attacker.p.x, 0, target.p.z - attacker.p.z};
    float l = std::max(.001f, Distance(target.p, attacker.p));
    target.push = Scale(d, skill.push / (l * .3f));
    target.pushEnd = world.now + .3;
    target.pushId = world.nextAction++;
    if (target.kind == EntityKind::Player)
      ++static_cast<Player &>(target).revision;
    e.velocity = target.push;
    e.duration = .3f;
    e.flags = 1;
  }
  e.mode = target.mode;
  e.revision = target.kind == EntityKind::Player
                   ? static_cast<Player &>(target).revision
                   : 0;
  if (target.kind == EntityKind::Player)
    static_cast<Player &>(target).dirty = true;
  Broadcast(target.map, e);
  if (target.kind == EntityKind::Player)
    Stats(static_cast<Player &>(target));
}
void GameplayProcessor::UpdateActor(Actor &a, double dt) {
  auto &m = world.maps.at(a.map);
  if (a.pushEnd > world.now) {
    Vec3 next = Add(a.p, Scale(a.push, static_cast<float>(dt)));
    next.y = m.surface.Sample(next.x, next.z);
    if (Geometry(m, a.p, next, a.radius) == Error::None) {
      a.p = next;
      a.velocity = a.push;
    } else {
      a.pushEnd = world.now;
      a.velocity = {};
    }
  } else if (a.pushId) {
    a.pushId = 0;
    a.velocity = {};
    if (a.kind == EntityKind::Player) {
      auto &p = static_cast<Player &>(a);
      ++p.revision;
      p.budget = 0;
      Broadcast(a.map, Base(EventKind::State, a));
    }
  }
  if (!a.action.id)
    return;
  const auto action = a.action;
  if (action.kind == ActionKind::Attack && !action.hit &&
      world.now >= action.impact) {
    a.action.hit = true;
    auto t = world.Target(action.target, a.map);
    const auto &skill = world.rules.skills.at(action.definition);
    if (skill.area > 0 && a.kind == EntityKind::Player) {
      for (auto &[id, target] : m.monsters)
        if (target.hp > 0 &&
            Distance(a.p, target.p) <= skill.area + target.radius &&
            Geometry(m, a.p, target.p, 0, true) == Error::None)
          Hit(a, target, skill);
    } else if (t && t->hp > 0 && a.hp > 0 &&
               world.InRange(a, *t, skill.reach) &&
               Geometry(m, a.p, t->p, 0, true) == Error::None)
      Hit(a, *t, skill);
  }
  if (!a.action.id || a.action.id != action.id || world.now < action.end)
    return;
  if (action.kind == ActionKind::Gather) {
    auto &p = static_cast<Player &>(a);
    auto i = m.resources.find(action.target);
    if (i == m.resources.end() || i->second.owner != p.id ||
        i->second.stock <= 0 ||
        Distance(p.p, i->second.p) > p.radius + i->second.radius + 1.2f) {
      Cancel(a);
      return;
    }
    auto &r = i->second;
    --r.stock;
    if (r.stock == 0 && r.refillDelay > 0) {
      r.refillAt = world.now + r.refillDelay;
      ++r.generation;
    }
    r.owner = 0;
    ++p.inventory[r.item];
    Inventory(p, r.item);
    Activity(p, 1,
             world.rules.items.count(r.item) ? world.rules.items.at(r.item).gatherKey
                                       : r.item);
    p.dirty = true;
    ResourceState(m, r);
  }
  if (action.kind == ActionKind::Craft) {
    auto &p = static_cast<Player &>(a);
    const auto &r = world.rules.recipes.at(action.definition);
    for (auto [id, n] : r.inputs)
      if (p.inventory[id] < n) {
        Cancel(a);
        return;
      }
    for (auto [id, n] : r.inputs) {
      p.inventory[id] -= n;
      Inventory(p, id);
    }
    p.inventory[r.output] += r.count;
    Inventory(p, r.output);
    Activity(p, 2,
             world.rules.items.count(r.output) ? world.rules.items.at(r.output).craftKey
                                         : r.output);
    p.dirty = true;
  }
  auto e = ActionEvent(EventKind::ActionEnded, a);
  a.action = {};
  if (a.mode == Mode::Looting)
    a.mode = Mode::NonCombat;
  e.mode = a.mode;
  Broadcast(a.map, e);
}
void GameplayProcessor::Advance(double dt) {
  if (dt <= 0 || dt > .25)
    return;
  world.now += dt;
  for (auto &[id, p] : world.players)
    if (p->epoch) {
      UpdateActor(*p, dt);
      Zones(*p);
      if (world.rules.production && p->tutorial == 1 &&
          Distance(p->p, world.rules.tutorialMove) <= world.rules.tutorialMoveRadius &&
          !(p->tutorialEvidence & 2))
        Activity(*p, 4, 0);
      if (p->dirty && world.now - p->lastSave >= 1)
        Save(*p);
    }
  for (auto &[mapId, map] : world.maps)
    for (auto &[id, m] : map.monsters) {
      UpdateActor(m, dt);
      if (auto point = refill.MonsterPosition(map, m)) {
        Broadcast(mapId, Base(EventKind::Despawn, m));
        refill.RefillMonster(m, *point);
        Spawn(m);
      }
      if (m.hp <= 0 || m.action.id || m.pushEnd > world.now)
        continue;
      Player *target = nullptr;
      float nearest = 10;
      for (auto &[pid, p] : world.players)
        if (p->epoch && p->map == mapId && p->hp > 0) {
          float d = Distance(m.p, p->p);
          if (d < nearest) {
            nearest = d;
            target = p.get();
          }
        }
      Vec3 goal = m.home;
      if (target) {
        goal = target->p;
        m.mode = Mode::Combat;
        auto s = world.rules.skills.at(1);
        if (world.InRange(m, *target, s.reach) && m.cooldowns[1] <= world.now) {
          m.cooldowns[1] = world.now + 1.8;
          Start(m, ActionKind::Attack, target->id, 1, s.duration, s.hit);
          continue;
        }
      } else {
        m.mode = Mode::NonCombat;
        goal = {
            m.home.x + std::sin(static_cast<float>(world.now * .3 + id)) * 2, 0,
            m.home.z + std::cos(static_cast<float>(world.now * .3 + id)) * 2};
      }
      Vec3 delta{goal.x - m.p.x, 0, goal.z - m.p.z};
      float d = Distance(goal, m.p);
      m.velocity = {};
      if (d > .2f && (!target || !world.InRange(m, *target, 1.2f))) {
        m.velocity = Scale(delta, m.speed / d);
        auto next = Add(m.p, Scale(m.velocity, static_cast<float>(dt)));
        next.y = map.surface.Sample(next.x, next.z);
        if (Geometry(map, m.p, next, m.radius) == Error::None)
          m.p = next;
        else
          m.velocity = {};
      }
    }
  if (world.now >= world.nextSnapshot) {
    world.nextSnapshot = world.now + .05;
    for (auto &[id, p] : world.players)
      if (p->epoch) {
        auto e = Base(EventKind::State, *p);
        Broadcast(p->map, e);
        p->velocity = {};
      }
    for (auto &[mid, m] : world.maps)
      for (auto &[id, a] : m.monsters)
        Broadcast(mid, Base(EventKind::State, a));
  }
}
int GameplayProcessor::AttackPower(const Actor &a) const {
  if (a.kind != EntityKind::Player)
    return 10;
  auto &p = static_cast<const Player &>(a);
  int result = p.baseAttack;
  for (auto [slot, id] : p.equipment)
    if (auto i = world.rules.items.find(id); i != world.rules.items.end() && slot == 1003)
      result += i->second.power;
  return result;
}
int GameplayProcessor::DefencePower(const Actor &a) const {
  if (a.kind != EntityKind::Player)
    return 0;
  auto &p = static_cast<const Player &>(a);
  int result = p.baseDefence;
  for (auto [slot, id] : p.equipment)
    if (auto i = world.rules.items.find(id); i != world.rules.items.end() && slot != 1003)
      result += i->second.power;
  return result;
}
void GameplayProcessor::Stats(Player &p) {
  auto e = Base(EventKind::Stats, p);
  e.definition = AttackPower(p);
  e.target = DefencePower(p);
  e.value = p.speed;
  e.duration = static_cast<float>(std::max(0.0, p.reviveAt - world.now));
  SendTo(p, e);
}
void GameplayProcessor::Equipment(Player &p, int slot) {
  auto e = Base(EventKind::Equipment, p);
  e.code = static_cast<uint16_t>(slot);
  auto i = p.equipment.find(slot);
  e.definition = i != p.equipment.end() ? i->second : 0;
  Broadcast(p.map, e);
}
void GameplayProcessor::QuestState(Player &p, int id) {
  auto &state = p.quests[id];
  auto &def = world.rules.quests.at(id);
  state.counts.resize(def.conditions.size());
  state.complete = !def.conditions.empty();
  for (size_t i = 0; i < def.conditions.size(); ++i)
    state.complete &= state.counts[i] >= def.conditions[i].required;
  for (size_t i = 0; i < std::max(size_t(1), def.conditions.size()); ++i) {
    auto e = Base(EventKind::Quest, p);
    e.definition = id;
    e.code = static_cast<uint16_t>(i);
    e.value = i < state.counts.size() ? static_cast<float>(state.counts[i]) : 0;
    e.flags = (state.complete ? 1 : 0) | (state.claimed ? 2 : 0);
    SendTo(p, e);
  }
}
void GameplayProcessor::TutorialState(Player &p) {
  auto e = Base(EventKind::Tutorial, p);
  e.definition = p.tutorial;
  e.flags = static_cast<uint16_t>(p.tutorialEvidence);
  SendTo(p, e);
}
void GameplayProcessor::Progress(Player &p, int kind, int target) {
  for (auto &[id, q] : world.rules.quests) {
    auto &state = p.quests[id];
    state.counts.resize(q.conditions.size());
    if (state.claimed)
      continue;
    bool changed = false;
    for (size_t i = 0; i < q.conditions.size(); ++i) {
      auto &c = q.conditions[i];
      if (c.kind == kind && (c.target == target || c.target == -1)) {
        state.counts[i] = std::min(c.required, state.counts[i] + 1);
        changed = true;
      }
    }
    if (changed)
      QuestState(p, id);
  }
}
void GameplayProcessor::Activity(Player &p, int kind, int target) {
  if (kind < 3)
    Progress(p, kind, target);
  int phase = kind == 0 ? 2 : kind == 1 ? 3 : kind == 2 ? 4 : kind == 3 ? 5 : 1;
  if (p.tutorial == phase) {
    p.tutorialEvidence |= 1u << phase;
    TutorialState(p);
  }
  auto e = Base(EventKind::Activity, p);
  e.code = static_cast<uint16_t>(kind);
  e.target = target;
  e.value = 1;
  SendTo(p, e);
  p.dirty = true;
}
void GameplayProcessor::ExecuteFeature(Player &p, const Request &r) {
  switch (r.kind) {
  case RequestKind::Equip: {
    auto &item = world.rules.items.at(r.definition);
    auto old = p.equipment.find(item.slot);
    if (old != p.equipment.end()) {
      ++p.inventory[old->second];
      Inventory(p, old->second);
    }
    --p.inventory[r.definition];
    p.equipment[item.slot] = r.definition;
    Inventory(p, r.definition);
    Equipment(p, item.slot);
    Stats(p);
    Activity(p, 3, r.definition);
    break;
  }
  case RequestKind::Unequip: {
    int id = p.equipment.at(r.definition);
    p.equipment.erase(r.definition);
    ++p.inventory[id];
    Inventory(p, id);
    Equipment(p, r.definition);
    Stats(p);
    break;
  }
  case RequestKind::UseItem: {
    auto &item = world.rules.items.at(r.definition);
    --p.inventory[r.definition];
    Inventory(p, r.definition);
    if (item.kind == 2) {
      p.hp = std::min(p.maxHp, p.hp + item.heal);
      Broadcast(p.map, Base(EventKind::State, p));
      Stats(p);
    } else {
      Actor a;
      a.id = world.nextPlacement++;
      a.kind = EntityKind::Placement;
      a.map = p.map;
      a.p = r.position;
      a.visual = r.definition;
      a.radius = 0;
      a.velocity = {0, static_cast<float>(r.target) / 100, 0};
      world.maps.at(p.map).placements.emplace(a.id, a);
      Spawn(a);
    }
    break;
  }
  case RequestKind::ClaimQuest: {
    auto &state = p.quests.at(r.definition);
    state.claimed = true;
    for (auto [id, n] : world.rules.quests.at(r.definition).rewards) {
      p.inventory[id] += n;
      Inventory(p, id);
    }
    QuestState(p, r.definition);
    break;
  }
  case RequestKind::ResourceRefill: {
    auto &resource = world.maps.at(p.map).resources.at(r.target);
    if (refill.RefillResource(resource)) {
      ResourceState(world.maps.at(p.map), resource);
    }
    break;
  }
  case RequestKind::Tutorial: {
    p.tutorial = p.tutorial >= 5 ? -1 : p.tutorial + 1;
    p.tutorialStart = world.now;
    p.tutorialEvidence = 0;
    TutorialState(p);
    break;
  }
  case RequestKind::Save:
  case RequestKind::Logout: {
    auto e = Base(EventKind::Saved, p);
    e.requestId = r.requestId;
    e.code = static_cast<uint16_t>(r.kind);
    e.error = Save(p, true) ? Error::None : Error::Restricted;
    SendTo(p, e);
    break;
  }
  default:
    break;
  }
}
bool GameplayProcessor::Save(Player &p, bool force) {
  if (!p.epoch)
    return true;
  if (!force && !p.dirty)
    return true;
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
  state.quests = p.quests;
  bool ok = true;
  try {
    if (p.save)
      ok = p.save(state);
  } catch (...) {
    ok = false;
  }
  p.lastSave = world.now;
  if (ok)
    p.dirty = false;
  return ok;
}
void GameplayProcessor::SaveAll() {
  for (auto &[id, p] : world.players)
    if (p->epoch)
      Save(*p, true);
}
void GameplayProcessor::Connect(
    int id, Send send, float moveSpeed, const AccountState &account,
    std::function<bool(const AccountState &)> save) {
  if (!world.players.count(id)) {
    auto p = std::make_shared<Player>();
    p->id = id;
    if (std::isfinite(moveSpeed) && moveSpeed > 0)
      p->speed = moveSpeed;
    p->send = std::move(send);
    p->account = account;
    p->save = std::move(save);
    p->budgetTime = world.now;
    world.players.emplace(id, std::move(p));
  }
}
void GameplayProcessor::Disconnect(int id) {
  auto i = world.players.find(id);
  if (i == world.players.end())
    return;
  auto &p = *i->second;
  if (p.epoch) {
    Cancel(p);
    Save(p, true);
    Broadcast(p.map, Base(EventKind::Despawn, p));
  }
  world.players.erase(i);
}
void GameplayProcessor::Join(Player &p) {
  if (!p.epoch) {
    p.epoch = 1;
    p.map = world.maps.count(1) && !world.rules.production
                ? 1
                : std::min_element(
                      world.maps.begin(), world.maps.end(),
                      [](auto &a, auto &b) { return a.first < b.first; })
                      ->first;
    if (p.account.loaded) {
      p.map = world.maps.count(p.account.map) ? p.account.map : p.map;
      p.hp = std::clamp(p.account.hp, 0.0f, p.account.maxHp);
      p.maxHp = p.account.maxHp;
      p.speed = p.account.speed;
      p.inventory = p.account.inventory;
      p.equipment = p.account.equipment;
      p.quests = p.account.quests;
      p.baseAttack = p.account.attack;
      p.baseDefence = p.account.defence;
      p.tutorial = p.account.tutorial;
    } else
      p.tutorial = world.rules.production ? 0 : -1;
    p.p = world.maps.at(p.map).spawn;
    if (p.account.loaded && p.account.hasPosition &&
        Geometry(world.maps.at(p.map), p.account.position, p.account.position,
                 p.radius) == Error::None)
      p.p = p.account.position;
    p.tutorialStart = world.now;
    p.mode = p.hp > 0 ? Mode::NonCombat : Mode::Dead;
    if (p.hp <= 0)
      p.reviveAt = world.now + world.rules.reviveDelay;
    for (auto &[id, q] : world.rules.quests) {
      auto &state = p.quests[id];
      state.counts.resize(q.conditions.size());
    }
    Spawn(p);
  }
}
Error GameplayProcessor::Execute(Player &p, const Request &r) {
  auto &map = world.maps.at(p.map);
  switch (r.kind) {
  case RequestKind::Move: {
    float distance = Distance(p.p, r.position);
    p.velocity = Scale({r.position.x - p.p.x, 0, r.position.z - p.p.z}, 20);
    p.budget -= distance;
    p.p = r.position;
    p.sequence = r.sequence;
    Zones(p);
    auto e = Base(EventKind::MoveResult, p);
    e.requestId = r.requestId;
    SendTo(p, e);
    break;
  }
  case RequestKind::Mode:
    p.mode = static_cast<Mode>(r.definition);
    Broadcast(p.map, Base(EventKind::State, p));
    break;
  case RequestKind::Attack: {
    auto s = world.rules.skills.at(r.definition);
    p.cooldowns[r.definition] = world.now + s.cooldown;
    Start(p, ActionKind::Attack, r.target, r.definition, s.duration, s.hit,
          r.requestId);
    break;
  }
  case RequestKind::Gather: {
    auto &resource = map.resources.at(r.target);
    resource.owner = p.id;
    p.mode = Mode::Looting;
    Start(p, ActionKind::Gather, r.target, resource.item, resource.gatherTime,
          resource.gatherTime, r.requestId);
    ResourceState(map, resource);
    break;
  }
  case RequestKind::Craft: {
    const auto &recipe = world.rules.recipes.at(r.definition);
    p.mode = Mode::Looting;
    Start(p, ActionKind::Craft, 0, r.definition, recipe.duration,
          recipe.duration, r.requestId);
    break;
  }
  case RequestKind::Cancel:
    Cancel(p);
    break;
  case RequestKind::ChangeMap: {
    Cancel(p);
    auto gone = Base(EventKind::Despawn, p);
    Broadcast(p.map, gone);
    p.map = r.definition;
    ++p.epoch;
    ++p.revision;
    p.sequence = 0;
    p.p = world.maps.at(p.map).spawn;
    p.velocity = {};
    p.pushEnd = 0;
    p.zones.clear();
    p.mode = p.hp > 0 ? Mode::NonCombat : Mode::Dead;
    Spawn(p);
    break;
  }
  case RequestKind::Revive:
    p.hp = p.maxHp;
    p.mode = Mode::NonCombat;
    p.p = world.maps.at(p.map).spawn;
    ++p.revision;
    Broadcast(p.map, Base(EventKind::State, p));
    break;
  case RequestKind::Probe: {
    bool found = false;
    for (auto &b : map.boxes)
      if (b.id == r.target) {
        found = true;
        break;
      }
    if (!found) {
      return Error::MissingTarget;
    }
    break;
  }
  default:
    ExecuteFeature(p, r);
    break;
  }

  p.dirty = true;
  return Error::None;
}
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
void GameplayProcessor::SetOutput(
    std::function<void(const Send &, const Event &)> callback) {
  output = std::move(callback);
}
void GameplayProcessor::RejectConnection(const ServiceWork &work) {
  Event event;
  event.kind = EventKind::Ack;
  event.requestId = work.request.requestId;
  event.error = Error::Unauthenticated;
  if (output) output(work.send, event);
}
void GameplayProcessor::OnValidated(const ValidationResult &result) {
  auto it = world.players.find(result.connectionId);
  if (it == world.players.end())
    return;
  auto &p = *it->second;
  const auto &r = result.request;
  if (result.disposition == RequestDisposition::Replay) {
    auto ack = p.cache.at(r.requestId).ack;
    ack.error = result.error;
    SendTo(p, ack);
    return;
  }
  auto ack = Base(EventKind::Ack, p);
  ack.requestId = r.requestId;
  if (result.disposition == RequestDisposition::PacketError) {
    ack.error = result.error;
    SendTo(p, ack);
    return;
  }
  ack.code = static_cast<uint16_t>(r.kind);
  if (result.disposition == RequestDisposition::Expired) {
    ack.error = result.error;
    SendTo(p, ack);
    return;
  }
  p.highRequest = r.requestId;
  Error error = result.error;
  if (result.disposition == RequestDisposition::Reject) {
    if (p.epoch) {
      Prepare(p);
      Reject(p, r, error);
    }
  } else if (r.kind == RequestKind::Join)
    Join(p);
  else {
    Prepare(p);
    error = Execute(p, r);
  }
  ack.error = error;
  ack.position = p.p;
  ack.revision = p.revision;
  ack.sequence = p.sequence;
  ack.mode = p.mode;
  ack.actionId = p.action.id;
  p.cache.emplace(r.requestId, CachedRequest{r, ack});
  p.order.push_back(r.requestId);
  if (p.order.size() > 512) {
    p.cache.erase(p.order.front());
    p.order.pop_front();
  }
  SendTo(p, ack);
  if (error == Error::None &&
      (r.kind == RequestKind::Join || r.kind == RequestKind::ChangeMap))
    Snapshot(p);
}
} // namespace EverWind::Gameplay
