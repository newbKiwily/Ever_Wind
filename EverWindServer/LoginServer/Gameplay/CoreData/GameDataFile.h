#pragma once
#include "GameDataHub.h"
#include <string>
namespace EverWind::Gameplay
{
class GameDataFile
{
  public:
    static GameDataHub Load(const std::string &path);
};
}
