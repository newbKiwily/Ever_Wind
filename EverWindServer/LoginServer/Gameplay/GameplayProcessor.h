#pragma once
#include "CoreData/WorldTypes.h"
namespace EverWind::Gameplay
{
class GameplayProcessor;
}
#include "Map/MapManager.h"
#include "Map/RefillSystem.h"
namespace EverWind::Gameplay
{
class GameplayProcessor
{
  private:
    MapManager &world;

    RefillSystem &refill;

    void Start(Actor &a, ActionKind kind, int target, int definition, float duration, float impact, uint64_t req = 0);

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

    bool Save(Player &p, bool force = false);

    Error SaveAndReply(Player &p, const Request &r);
  public:
    explicit GameplayProcessor(MapManager &state, RefillSystem &refillSystem);

    void Process(const GameWork &work);

    void RejectPacket(const GameWork &work, Error error);

    void RejectConnection(const GameWork &work);

    void Connect(int id, Send send, float speed, const AccountState &account, std::function<bool(const AccountState &)> save, std::shared_ptr<Player> player);

    void Disconnect(int id);

    void SaveAll();

    void Advance(double dt);

    Error HandleJoinRequest(Player &p, const Request &r);

    Error HandleMoveRequest(Player &p, const Request &r);

    Error HandleModeRequest(Player &p, const Request &r);

    Error HandleAttackRequest(Player &p, const Request &r);

    Error HandleGatherRequest(Player &p, const Request &r);

    Error HandleCraftRequest(Player &p, const Request &r);

    Error HandleCancelRequest(Player &p, const Request &r);

    Error HandleChangeMapRequest(Player &p, const Request &r);

    Error HandleReviveRequest(Player &p, const Request &r);

    Error HandleProbeRequest(Player &p, const Request &r);

    Error HandleUseItemRequest(Player &p, const Request &r);

    Error HandleEquipRequest(Player &p, const Request &r);

    Error HandleUnequipRequest(Player &p, const Request &r);

    Error HandleClaimQuestRequest(Player &p, const Request &r);

    Error HandleResourceRefillRequest(Player &p, const Request &r);

    Error HandleTutorialRequest(Player &p, const Request &r);

    Error HandleSaveRequest(Player &p, const Request &r);

    Error HandleLogoutRequest(Player &p, const Request &r);

    void Join(Player &p);

    void Prepare(Player &p);

    void Reject(Player &p, const Request &r, Error error);

    Event Base(EventKind kind, const Actor &a) const;

    void SendTo(Player &p, Event e);

    void Snapshot(Player &p);
};
}
