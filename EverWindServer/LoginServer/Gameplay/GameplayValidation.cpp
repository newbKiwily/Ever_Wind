#include "GameplayValidation.h"
#include "Validation/Ingame/ActionIdleValidator.h"
#include "Validation/Ingame/AliveRequestValidator.h"
#include "Validation/Ingame/AttackCooldownValidator.h"
#include "Validation/Ingame/AttackRangeValidator.h"
#include "Validation/Ingame/AttackTargetValidator.h"
#include "Validation/Ingame/CancelActionValidator.h"
#include "Validation/Ingame/CombatModeValidator.h"
#include "Validation/Ingame/CraftIngredientsValidator.h"
#include "Validation/Ingame/EquipmentSlotValidator.h"
#include "Validation/Ingame/EquipmentTypeValidator.h"
#include "Validation/Ingame/ForcedMovementValidator.h"
#include "Validation/Ingame/GatherRangeValidator.h"
#include "Validation/Ingame/ItemExistsValidator.h"
#include "Validation/Ingame/ItemOwnershipValidator.h"
#include "Validation/Ingame/ItemUseValidator.h"
#include "Validation/Ingame/MapExistsValidator.h"
#include "Validation/Ingame/ModeRequestValidator.h"
#include "Validation/Ingame/MoveSequenceValidator.h"
#include "Validation/Ingame/MovementBudgetValidator.h"
#include "Validation/Ingame/MovementGeometryValidator.h"
#include "Validation/Ingame/NonCombatModeValidator.h"
#include "Validation/Ingame/QuestRewardValidator.h"
#include "Validation/Ingame/RecipeExistsValidator.h"
#include "Validation/Ingame/ResourceAvailabilityValidator.h"
#include "Validation/Ingame/ResourceExistsValidator.h"
#include "Validation/Ingame/ResourceRefillValidator.h"
#include "Validation/Ingame/ReviveDelayValidator.h"
#include "Validation/Ingame/ReviveStateValidator.h"
#include "Validation/Ingame/SkillExistsValidator.h"
#include "Validation/Ingame/TutorialAttackValidator.h"
#include "Validation/Ingame/TutorialMapValidator.h"
#include "Validation/Ingame/TutorialMovementValidator.h"
#include "Validation/Ingame/TutorialProgressValidator.h"
#include "Validation/Ingame/WorldEpochValidator.h"
namespace EverWind::Gameplay {
GameplayValidation::GameplayValidation(WorldState &state, RefillSystem &refillSystem) : world(state), refill(refillSystem) {
  Initialize();
}
void GameplayValidation::Initialize() {

  common = std::make_unique<WorldEpochValidator>(world);
  common->Then(std::make_unique<AliveRequestValidator>(world))
      .Then(std::make_unique<ForcedMovementValidator>(world));
  specific[RequestKind::Move] = std::make_unique<MoveSequenceValidator>(world);
  specific[RequestKind::Move]
      ->Then(std::make_unique<ActionIdleValidator>(world))
      .Then(std::make_unique<MovementGeometryValidator>(world))
      .Then(std::make_unique<MovementBudgetValidator>(world))
      .Then(std::make_unique<TutorialMovementValidator>(world));

  specific[RequestKind::Attack] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::Attack]
      ->Then(std::make_unique<SkillExistsValidator>(world))
      .Then(std::make_unique<CombatModeValidator>(world))
      .Then(std::make_unique<AttackTargetValidator>(world))
      .Then(std::make_unique<AttackRangeValidator>(world))
      .Then(std::make_unique<AttackCooldownValidator>(world))
      .Then(std::make_unique<TutorialAttackValidator>(world));

  specific[RequestKind::Gather] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::Gather]
      ->Then(std::make_unique<NonCombatModeValidator>(world))
      .Then(std::make_unique<ResourceExistsValidator>(world))
      .Then(std::make_unique<GatherRangeValidator>(world))
      .Then(std::make_unique<ResourceAvailabilityValidator>(world));

  specific[RequestKind::Craft] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::Craft]
      ->Then(std::make_unique<NonCombatModeValidator>(world))
      .Then(std::make_unique<RecipeExistsValidator>(world))
      .Then(std::make_unique<CraftIngredientsValidator>(world));

  specific[RequestKind::Cancel] =
      std::make_unique<CancelActionValidator>(world);

  specific[RequestKind::ChangeMap] =
      std::make_unique<MapExistsValidator>(world);
  specific[RequestKind::ChangeMap]->Then(
      std::make_unique<TutorialMapValidator>(world));

  specific[RequestKind::Revive] = std::make_unique<ReviveStateValidator>(world);
  specific[RequestKind::Revive]->Then(
      std::make_unique<ReviveDelayValidator>(world));

  specific[RequestKind::Mode] = std::make_unique<ModeRequestValidator>(world);

  specific[RequestKind::UseItem] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::UseItem]
      ->Then(std::make_unique<ItemExistsValidator>(world))
      .Then(std::make_unique<ItemOwnershipValidator>(world))
      .Then(std::make_unique<ItemUseValidator>(world));

  specific[RequestKind::Equip] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::Equip]
      ->Then(std::make_unique<ItemExistsValidator>(world))
      .Then(std::make_unique<ItemOwnershipValidator>(world))
      .Then(std::make_unique<EquipmentTypeValidator>(world));

  specific[RequestKind::Unequip] = std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::Unequip]->Then(
      std::make_unique<EquipmentSlotValidator>(world));

  specific[RequestKind::ClaimQuest] =
      std::make_unique<ActionIdleValidator>(world);
  specific[RequestKind::ClaimQuest]->Then(
      std::make_unique<QuestRewardValidator>(world));

  specific[RequestKind::ResourceRefill] =
      std::make_unique<ResourceRefillValidator>(world, refill);

  specific[RequestKind::Tutorial] =
      std::make_unique<TutorialProgressValidator>(world);
}
Error GameplayValidation::ValidatePacket(const Request &r) const {
  return packet.Contract(r);
}
Error GameplayValidation::Validate(const Context &c) const {
  auto error = common->Check(c);
  if (error != Error::None)
    return error;
  auto i = specific.find(c.request.kind);
  return i != specific.end() ? i->second->Check(c) : Error::None;
}
bool GameplayValidation::InsertBefore(RequestKind kind, std::string_view anchor,
                                      std::unique_ptr<IngameValidator> stage) {
  auto i = specific.find(kind);
  return i != specific.end() &&
         IngameValidator::InsertBefore(i->second, anchor, std::move(stage));
}
} // namespace EverWind::Gameplay

#include <cstring>
#include <iostream>
#include <syncstream>
namespace {
const char *ErrorName(EverWind::Gameplay::Error error) {
  static constexpr const char *names[] = {
      "None", "Unauthenticated", "BadRequest", "WrongWorld", "Stale", "Dead",
      "Restricted", "OutOfRange", "MissingTarget", "Cooldown", "Busy", "Exhausted",
      "Ingredients", "Collision", "Speed", "DuplicateConflict", "Expired", "Cancelled"};
  auto index = static_cast<unsigned>(error);
  return index < sizeof names / sizeof *names ? names[index] : "Unknown";
}
void LogInvalidFrame(const EverWind::Gameplay::ServiceWork &work, EverWind::Gameplay::Error error) {
  std::osyncstream(std::cout) << "[Packet Rejected] connection=" << work.id
      << " packet=" << work.packetId << " bytes=" << work.payload.size()
      << " reason=" << ErrorName(error) << '\n';
}
}
namespace EverWind::Gameplay {
Error GameplayValidation::Decode(uint16_t id, const char *payload, size_t size,
                                 bool authenticated, Request &request) const {
  PacketContext c;
  c.id = id;
  c.payload = payload;
  c.size = size;
  c.authenticated = authenticated;
  auto error = packet.Envelope(c);
  if (error != Error::None)
    return error;
  std::memcpy(&request, payload, sizeof request);
  return Error::None;
}

void GameplayValidation::SetNext(
    std::function<void(const ValidationResult &)> callback) {
  next = std::move(callback);
}
void GameplayValidation::SetInvalidPacket(
    std::function<void(const ServiceWork &)> callback) {
  invalidPacket = std::move(callback);
}
bool GameplayValidation::DecodeInput(ServiceWork &work) const {
  if (!work.raw)
    return true;
  if (auto error = Decode(work.packetId, work.payload.data(), work.payload.size(),
             work.authenticated, work.request); error != Error::None) {
    LogInvalidFrame(work, error);
    if (invalidPacket)
      invalidPacket(work);
    return false;
  }
  work.raw = false;
  return true;
}
void GameplayValidation::OnReceived(const ServiceWork &work) const {
  Request request = work.request;
  if (work.raw) {
    auto error = Decode(work.packetId, work.payload.data(), work.payload.size(), work.authenticated, request);
    if (error != Error::None) {
      LogInvalidFrame(work, error);
      if (invalidPacket) invalidPacket(work);
      return;
    }
  }
  auto it = world.players.find(work.id);
  if (it == world.players.end())
    return;
  const auto &p = *it->second;
  ValidationResult result{work.id, request};
  if (auto error = ValidatePacket(request); error != Error::None) {
    result.disposition = RequestDisposition::PacketError;
    result.error = error;
  } else if (auto cached = p.cache.find(request.requestId);
             cached != p.cache.end()) {
    result.disposition = RequestDisposition::Replay;
    result.error = Same(cached->second.request, request)
                       ? cached->second.ack.error
                       : Error::DuplicateConflict;
  } else if (request.requestId <= p.highRequest) {
    result.disposition = RequestDisposition::Expired;
    result.error = Error::Expired;
  } else if (request.kind != RequestKind::Join) {
    result.error = !p.epoch
                       ? Error::Unauthenticated
                       : Validate(Context{*it->second, world.maps.at(p.map),
                                          request, world.now});
    if (result.error != Error::None)
      result.disposition = RequestDisposition::Reject;
  }
  if (request.kind == RequestKind::Move && result.error != Error::None)
    std::osyncstream(std::cout) << "[Move Rejected] connection=" << work.id
        << " request=" << request.requestId << " sequence=" << request.sequence
        << " epoch=" << request.epoch << " revision=" << request.revision
        << " reason=" << ErrorName(result.error)
        << " position=(" << request.position.x << ',' << request.position.y << ',' << request.position.z << ")\n";
  if (next)
    next(result);
}
} // namespace EverWind::Gameplay
