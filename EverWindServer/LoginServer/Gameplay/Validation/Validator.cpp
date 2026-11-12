#include "Validator.h"
#include <stdexcept>
#include <utility>
namespace EverWind::Gameplay
{
Validator::Validator(std::string_view value) : name(value)
{
}
Validator::~Validator() = default;
Error Validator::Check(const PacketContext &context) const
{
    const auto error = Validate(context);
    return error == Error::None && next ? next->Check(context) : error;
}
Error Validator::Check(const Context &context) const
{
    const auto error = Validate(context);
    return error == Error::None && next ? next->Check(context) : error;
}
Validator &Validator::Then(std::unique_ptr<Validator> stage)
{
    if (!stage || next)
        throw std::logic_error("Chain stage already connected or empty");
    next = std::move(stage);
    return *next;
}
bool Validator::InsertBefore(std::unique_ptr<Validator> &head, std::string_view anchor,
                             std::unique_ptr<Validator> stage)
{
    if (!stage || stage->next)
        return false;
    auto link = &head;
    while (*link)
    {
        if ((*link)->name == anchor)
        {
            stage->next = std::move(*link);
            *link = std::move(stage);
            return true;
        }
        link = &(*link)->next;
    }
    return false;
}
} // namespace EverWind::Gameplay
