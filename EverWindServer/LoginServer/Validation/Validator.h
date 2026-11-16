#pragma once
#include "../Gameplay/CoreData/WorldTypes.h"
#include <memory>
#include <string_view>
namespace EverWind::Gameplay
{
class Validator
{
  private:
    std::string_view name;

    std::unique_ptr<Validator> next;
  protected:
    explicit Validator(std::string_view name);

    virtual Error Validate(const ValidationContext &context) const = 0;
  public:
    virtual ~Validator();

    Error Check(const ValidationContext &context) const;

    Validator &Then(std::unique_ptr<Validator> stage);

    static bool InsertBefore(std::unique_ptr<Validator> &head, std::string_view anchor, std::unique_ptr<Validator> stage);
};
}
