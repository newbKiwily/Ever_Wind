#pragma once
#include "../DB/Queries.h"
#include "SessionManager.h"
#include "../Gameplay/Map/MapManager.h"
#include <functional>
// 패킷 종류별로 분배하며 게임 상태는 맵 관리자의 게임 스레드에서만 변경한다.
class PacketMethod
{
  private:
    std::shared_ptr<Queries> query;

    SessionManager &sessions;

    EverWind::Gameplay::MapManager &maps;

    bool HandleLoginRequest(Session *, const NetPackets::PKT_LOGIN_REQ &);

    bool HandleSignUpRequest(Session *, const NetPackets::PKT_SIGNUP_REQ &);

    std::vector<char> BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y, float z, int tutorialStep);

    std::vector<char> BuildSignUpAck(int resultCode);

    std::vector<char> BuildStatAck(int atk, int def, float speed, float hp, float max_hp);

    std::vector<char> BuildInventoryAck(const GameStruct::InventoryItem &item);

    std::vector<char> BuildQuestInfoAck(const QuestSaveData &questData);

    EverWind::Gameplay::GameWork
    MakeGameWork(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleJoinPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleMovePacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleModePacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleAttackPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleGatherPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleCraftPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleCancelPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleChangeMapPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleRevivePacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleProbePacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleUseItemPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleEquipPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleUnequipPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleClaimQuestPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleResourceRefillPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleTutorialPacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleSavePacket(Session &session, const EverWind::Gameplay::Request &request);

    bool HandleLogoutPacket(Session &session, const EverWind::Gameplay::Request &request);
  public:
    PacketMethod(std::shared_ptr<Queries> queries, SessionManager &connections, EverWind::Gameplay::MapManager &manager);

    bool HandlePacket(Session *session, const NetPackets::PacketHeader &header, const char *payload, size_t payloadSize);
};
