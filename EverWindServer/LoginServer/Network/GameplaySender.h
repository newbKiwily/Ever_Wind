#pragma once
#include "../Gameplay/Protocol.h"
#include "Session.h"
#include <memory>
class GameplaySender
{
  public:
    static EverWind::Gameplay::Send Create(std::weak_ptr<Session> session);
};
