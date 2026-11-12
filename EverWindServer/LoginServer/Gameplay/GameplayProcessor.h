#pragma once
#include "Map/WorldState.h"
#include "Refill/RefillSystem.h"
namespace EverWind::Gameplay {
class GameplayProcessor {
public:
  explicit GameplayProcessor(WorldState &state, RefillSystem &refillSystem);
  void SetOutput(std::function<void(const Send &, const Event &)> callback);
  void OnValidated(const ValidationResult &result);
  void RejectConnection(const ServiceWork &work);
  void Connect(int id, Send send, float speed, const AccountState &account,
               std::function<bool(const AccountState &)> save);
  void Disconnect(int id);
  void SaveAll();
  void Advance(double dt);
  void Join(Player &p);
  Error Execute(Player &p, const Request &r);
  void Prepare(Player &p);
  void Reject(Player &p, const Request &r, Error error);

  Event Base(EventKind kind, const Actor &a) const;
  void SendTo(Player &p, Event e);
  void Snapshot(Player &p);

private:
  void Start(Actor &a, ActionKind kind, int target, int definition,
             float duration, float impact, uint64_t req = 0);
  void Broadcast(int map, Event e);
  void Inventory(Player &p, int item);
  void ResourceState(Map &map, Resource &r, Player *only = nullptr);
  void Spawn(Actor &a, Player *only = nullptr);
  Event ActionEvent(EventKind kind, const Actor &a);
  void Cancel(Actor &a, Error reason = Error::Cancelled);
  void Zones(Player &p);
  void Hit(Actor &attacker, Actor &target, const Skill &skill);
  void UpdateActor(Actor &a, double dt);
  void Stats(Player &p);
  void Equipment(Player &p, int slot);
  void QuestState(Player &p, int quest);
  void TutorialState(Player &p);
  void Activity(Player &p, int kind, int target);
  void Progress(Player &p, int kind, int target);
  int AttackPower(const Actor &a) const;
  int DefencePower(const Actor &a) const;
  void ExecuteFeature(Player &p, const Request &r);
  bool Save(Player &p, bool force = false);

private:
  std::function<void(const Send &, const Event &)> output;
  WorldState &world;
  RefillSystem &refill;
};
} // namespace EverWind::Gameplay
