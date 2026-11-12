#include "GameplaySender.h"
EverWind::Gameplay::Send GameplaySender::Create(std::weak_ptr<Session> session)
{
    return [session](const EverWind::Gameplay::Event &event) {
        if (auto connection = session.lock(); connection && !connection->IsClosed())
        {
            const auto bytes = EverWind::Gameplay::Encode(EverWind::Gameplay::EventPacket, event);
            connection->PostSend(bytes.data(), bytes.size());
        }
    };
}
