#pragma once
#include "Validator.h"
#include <type_traits>
#include <utility>
namespace EverWind::Gameplay
{
class Validation
{
  private:
    std::unique_ptr<Validator> first;

    Validator *last = nullptr;

    Validation() = default;
  public:
    Validation &operator=(Validation &&other) noexcept;

    Validation &operator=(const Validation &) = delete;

    static Validation Chain();

    Validation(Validation &&other) noexcept;
    Validation(const Validation &) = delete;

    template <typename T, typename... Args>
    Validation &Then(Args &&...args)
    {
        static_assert(std::is_base_of_v<Validator, T>);

        auto stage = std::make_unique<T>(std::forward<Args>(args)...);

        auto *tail = stage.get();

        if (last)
        {
            last->Then(std::move(stage));
        }
        else
        {
            first = std::move(stage);
        }

        last = tail;

        return *this;
    }

    Error Check(const ValidationContext &context) const;
};
}
