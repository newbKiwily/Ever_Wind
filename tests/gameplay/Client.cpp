#include "Client.h"

Client::Client(ServerRuntime &c, int id, float moveSpeed,const AccountState& account,std::function<bool(const AccountState&)> save) : core(c), id(id)
{
    core.Connect(id, [this](const Event &e) {
        events.push_back(e);
        if (e.entity == this->id)
        {
            epoch = e.epoch;
            if (e.kind == EventKind::MoveResult || e.kind == EventKind::Welcome || e.kind == EventKind::State)
            {
                revision = e.revision;
                position = e.position;
            }
        }
    }, moveSpeed,account,std::move(save));
}

Request Client::Make(RequestKind kind)
{
    Request r;
    r.kind = kind;
    r.requestId = ++next;
    r.epoch = epoch;
    r.revision = revision;
    return r;
}

Event Client::Send(Request r)
{
    core.Handle(id, r);
    for (auto i = events.rbegin(); i != events.rend(); ++i)
        if (i->kind == EventKind::Ack && i->requestId == r.requestId)
            return *i;
    assert(false);
    return {};
}

void Client::Join()
{
    assert(Send(Make(RequestKind::Join)).error == Error::None);
}

void Client::MoveTo(Vec3 target)
{
    for (int n = 0; n < 300 && std::hypot(position.x - target.x, position.z - target.z) > .1f; ++n)
    {
        core.Advance(.1);
        float dx = target.x - position.x, dz = target.z - position.z, d = std::hypot(dx, dz);
        float step = std::min(.4f, d);
        auto r = Make(RequestKind::Move);
        r.sequence = ++seq;
        r.position = {position.x + dx / d * step, 0, position.z + dz / d * step};
        auto ack = Send(r);
        assert(ack.error == Error::None);
    }
}

size_t Client::Count(EventKind kind)
{
    return std::count_if(events.begin(), events.end(), [&](auto &e) { return e.kind == kind; });
}
