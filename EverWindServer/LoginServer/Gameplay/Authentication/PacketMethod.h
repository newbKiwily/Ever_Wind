#pragma once
#include "../../DB/Queries.h"
#include "../../Network/SessionManager.h"
#include <functional>
// Authentication adapter. Game entities are handled exclusively by the gameplay pipeline.
class PacketMethod
{
  public:
    PacketMethod(Queries &queries, SessionManager &connections, std::function<bool(int)> exists);
    bool HandlePacket(Session *session, const NetPackets::PacketHeader &header, const char *payload,
                      size_t payloadSize);

  private:
    bool HandleLoginRequest(Session *, const NetPackets::PKT_LOGIN_REQ &);
    bool HandleSignUpRequest(Session *, const NetPackets::PKT_SIGNUP_REQ &);
    std::vector<char> BuildLoginAck(int resultCode, int userDbId, int mapId, float x, float y, float z,
                                    int tutorialStep);
    std::vector<char> BuildSignUpAck(int resultCode);
    std::vector<char> BuildStatAck(int atk, int def, float speed, float hp, float max_hp);
    std::vector<char> BuildInventoryAck(const GameStruct::InventoryItem &item);
    std::vector<char> BuildQuestInfoAck(const QuestSaveData &questData);
    Queries &query;
    SessionManager &sessions;
    std::function<bool(int)> mapExists;
};
