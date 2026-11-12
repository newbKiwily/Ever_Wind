#pragma once
#include "Protocol.h"
#include <cstdint>
#include <deque>
#include <functional>
#include <memory>
#include <string>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>

namespace EverWind::Gameplay {
struct ItemDefinition {
  std::string key;
  int kind = 0, slot = 0, power = 0, gatherKey = 0, craftKey = 0;
  float heal = 0;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct QuestCondition {
  int kind = 0, target = 0, required = 0;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct QuestDefinition {
  int id = 0;
  std::vector<QuestCondition> conditions;
  std::vector<std::pair<int, int>> rewards;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct TutorialDefinition {
  int id = 0, next = -1, eventKind = -1, target = -1;
  float minimumSeconds = 0, radius = 2;
  Vec3 position{};
  bool allowMove = true, allowAttack = true;
};
struct QuestProgress {
  std::vector<int> counts;
  bool complete = false, claimed = false;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Recipe {
  float duration;
  int output, count;
  std::vector<std::pair<int, int>> inputs;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Skill {
  float duration, hit, reach, damage, cooldown, push;
  float ratio = -1, area = 0;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Action {
  uint64_t id = 0;
  ActionKind kind = ActionKind::None;
  int target = 0, definition = 0;
  double start = 0, end = 0, impact = 0;
  bool hit = false;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Actor {
  int id = 0, map = 1, visual = 0;
  EntityKind kind = EntityKind::Player;
  Vec3 p{}, velocity{}, home{};
  float radius = .45f, speed = 5, hp = 100, maxHp = 100;
  Mode mode = Mode::NonCombat;
  Action action{};
  std::unordered_map<int, double> cooldowns;
  Vec3 push{};
  double pushEnd = 0;
  uint64_t pushId = 0;
  double diedAt = 0;
  float respawnSeconds = 10;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Box {
  int id;
  float x, z, hx, hz;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Resource {
  int id = 0, item = 0, stock = 0, owner = 0;
  Vec3 p{};
  float radius = 0;
  int capacity = 1;
  unsigned generation = 0;
  float gatherTime = 2, refillDelay = 0;
  double refillAt = 0;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Zone {
  int id;
  Vec3 p;
  float radius;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct HeightField {
  float x = 0, z = 0, step = 1, flat = 0;
  int columns = 0, rows = 0;
  std::vector<float> samples;
  float Sample(float px, float pz) const;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Map {
  int id = 0, version = 1;
  float defaultMonsterRespawnSeconds = 10;
  float radius = 24;
  std::vector<Box> boxes;
  std::vector<Zone> zones;
  std::unordered_map<int, Actor> monsters;
  std::unordered_map<int, Resource> resources;
  Vec3 center{}, spawn{0, 0, -10};
  HeightField surface;
  Vec3 monsterCenter{};
  float monsterRadius = 0;
  std::unordered_map<int, Actor> placements;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct AccountState {
  bool loaded = false, hasPosition = false;
  int map = 0, tutorial = 0, attack = 50, defence = 20;
  float hp = 100, maxHp = 100, speed = 4.5f;
  Vec3 position{};
  std::unordered_map<int, int> inventory, equipment;
  std::unordered_map<int, QuestProgress> quests;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct CachedRequest {
  Request request;
  Event ack;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Player : Actor {
  Send send;
  uint32_t epoch = 0, revision = 0, sequence = 0;
  uint64_t highRequest = 0;
  float budget = 1;
  double budgetTime = 0;
  std::unordered_map<int, int> inventory;

  std::unordered_map<uint64_t, CachedRequest> cache;
  std::deque<uint64_t> order;
  std::unordered_set<int> zones;
  AccountState account;
  std::function<bool(const AccountState &)> save;
  int baseAttack = 50, baseDefence = 20, tutorial = -1;
  unsigned tutorialEvidence = 0;
  double tutorialStart = 0, reviveAt = 0, lastSave = 0;
  bool dirty = false;
  std::unordered_map<int, int> equipment;
  std::unordered_map<int, QuestProgress> quests;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
struct Context {
  Player &player;
  Map &map;
  const Request &request;
  double now;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
enum class RequestDisposition { Execute, Reject, Replay, PacketError, Expired };
struct ValidationResult {
  int connectionId;
  Request request;
  Error error = Error::None;
  RequestDisposition disposition = RequestDisposition::Execute;
};
struct ServiceWork {
  int id;
  Request request;
  Send send;
  std::function<bool()> alive;
  float moveSpeed = 5.0f;
  std::shared_ptr<const AccountState> account;
  std::function<bool(const AccountState &)> save;
  std::function<AccountState()> load;
  bool raw = false;
  uint16_t packetId = 0;
  bool authenticated = false;
  std::vector<char> payload;
  std::function<void()> close;
};
} // namespace EverWind::Gameplay

namespace EverWind::Gameplay {
float Distance(Vec3 a, Vec3 b);
Vec3 Add(Vec3 a, Vec3 b);
Vec3 Scale(Vec3 a, float v);
bool Finite(Vec3 a);
bool Crosses(Vec3 a, Vec3 b, const Box &box, float radius);
// Movement ignores entities; non-movement visibility/placement checks can retain resource blocking.
Error Geometry(const Map &map, Vec3 from, Vec3 to, float radius, bool blockResources = false);
bool Same(const Request &a, const Request &b);
} // namespace EverWind::Gameplay
