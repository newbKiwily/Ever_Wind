#include "Validation.h"
namespace EverWind::Gameplay
{
Validation::Validation(Validation &&other) noexcept
    : first(std::move(other.first)), last(std::exchange(other.last, nullptr))
{
}

Validation &Validation::operator=(Validation &&other) noexcept
{
    if (this != &other)
    {
        first = std::move(other.first);

        last = std::exchange(other.last, nullptr);
    }

    return *this;
}

Validation Validation::Chain()
{
    return Validation{};
}

Error Validation::Check(const ValidationContext &context) const
{
    // 비어 있는 검증 체인은 행동을 승인하지 않는다.
    return first ? first->Check(context) : Error::BadRequest;
}
}
