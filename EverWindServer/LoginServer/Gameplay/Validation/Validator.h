#pragma once
#include "../WorldTypes.h"
#include <memory>
#include <string_view>
namespace EverWind::Gameplay
{
class Validator
{
  public:
    virtual ~Validator();
    Error Check(const PacketContext &context) const;
    Error Check(const Context &context) const;
    Validator &Then(std::unique_ptr<Validator> stage);
    static bool InsertBefore(std::unique_ptr<Validator> &head, std::string_view anchor,
                             std::unique_ptr<Validator> stage);

  protected:
    explicit Validator(std::string_view name);
    virtual Error Validate(const PacketContext &context) const = 0;
    virtual Error Validate(const Context &context) const = 0;

  private:
    std::string_view name;
    std::unique_ptr<Validator> next;
};
} // namespace EverWind::Gameplay
