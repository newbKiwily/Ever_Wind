#pragma once
#include "PacketValidation.h"
#include "Validation/IngameValidator.h"
#include "Refill/RefillSystem.h"
namespace EverWind::Gameplay {
class GameplayValidation {
public:
  explicit GameplayValidation(WorldState &state, RefillSystem &refillSystem);
  void SetNext(std::function<void(const ValidationResult &)> callback);
  void SetInvalidPacket(std::function<void(const ServiceWork &)> callback);
  void OnReceived(const ServiceWork &work) const;
  bool DecodeInput(ServiceWork &work) const;
  Error Decode(uint16_t id, const char *payload, size_t size,
               bool authenticated, Request &request) const;
  Error ValidatePacket(const Request &r) const;
  Error Validate(const Context &c) const;
  bool InsertBefore(RequestKind kind, std::string_view anchor,
                    std::unique_ptr<IngameValidator> stage);

private:
  void Initialize();
  std::function<void(const ValidationResult &)> next;
  std::function<void(const ServiceWork &)> invalidPacket;
  WorldState &world;
  RefillSystem &refill;
  PacketValidation packet;
  std::unique_ptr<Validator> common;
  std::unordered_map<RequestKind, std::unique_ptr<Validator>> specific;
};
} // namespace EverWind::Gameplay
