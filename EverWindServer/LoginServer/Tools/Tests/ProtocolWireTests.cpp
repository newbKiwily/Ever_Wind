#include "../../Network/GameplayPackets.h"
#include "../../Validation/PacketValidator.h"
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
using namespace EverWind::Gameplay;
void Check(bool value)
{
    if (!value)
    {
        throw std::runtime_error("Protocol assertion failed");
    }
}
std::vector<char> Read(const std::string &path)
{
    std::ifstream input(path, std::ios::binary);
    Check(bool(input));
    return {std::istreambuf_iterator<char>(input), std::istreambuf_iterator<char>()};
}
int main()
{
    const std::string folder = "Temp/PacketMigration/fixtures/";
    int count = 0;
    {
        auto bytes = Read(folder + "C2S_JOIN_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1537 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Join);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_MOVE_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1538 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Move);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.sequence == 104);
        Check(request.position.x == 1.25f && request.position.y == -2.5f && request.position.z == 3.75f);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_MODE_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1539 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Mode);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_ATTACK_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1540 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Attack);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.target == 106);
        Check(request.definition == 107);
        Check(request.position.x == 1.25f && request.position.y == -2.5f && request.position.z == 3.75f);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_GATHER_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1541 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Gather);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.target == 106);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_CRAFT_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1542 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Craft);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_CANCEL_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1543 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Cancel);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.actionId == 4294967301ULL);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_CHANGE_MAP_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1544 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::ChangeMap);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_REVIVE_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1545 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Revive);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_PROBE_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1546 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Probe);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.target == 106);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_USE_ITEM_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1547 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::UseItem);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        Check(request.target == 106);
        Check(request.position.x == 1.25f && request.position.y == -2.5f && request.position.z == 3.75f);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_EQUIP_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1548 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Equip);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_UNEQUIP_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1549 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Unequip);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_CLAIM_QUEST_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1550 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::ClaimQuest);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_TUTORIAL_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1551 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Tutorial);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_RESOURCE_REFILL_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1552 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::ResourceRefill);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.target == 106);
        Check(request.sequence == 104);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_SAVE_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1553 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Save);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_LOGOUT_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1554 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::Logout);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_ATTACK_TARGETS_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1555 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::AttackTargets);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.actionId == 4294967301ULL);
        Check(request.definition == 107);
        Check(request.sequence == 104);
        Check(request.targetCount == 64);
        for (int i = 0; i < 64; ++i)
        {
            Check(request.targets[i] == 1000 + i);
        }
        request.targetCount = 65;
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::BadRequest);
        auto empty = Read(folder + "ATTACK_TARGETS_EMPTY.bin");
        Check(DecodeRequest(header.Id, empty.data() + 4, empty.size() - 4, request));
        Check(request.targetCount == 0);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_UPGRADE_STAT_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1556 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::UpgradeStat);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_UPGRADE_SKILL_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1557 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::UpgradeSkill);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        Check(request.definition == 107);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_RESET_STATS_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1558 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::ResetStats);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    {
        auto bytes = Read(folder + "C2S_RESET_SKILLS_REQUEST.bin");
        NetPackets::PacketHeader header{};
        std::memcpy(&header, bytes.data(), sizeof header);
        Check(header.Id == 1559 && header.Length == bytes.size());
        Request request;
        const char *payload = bytes.data() + sizeof header;
        size_t size = bytes.size() - sizeof header;
        Check(DecodeRequest(header.Id, payload, size, request));
        Check(request.kind == RequestKind::ResetSkills);
        Check(!DecodeRequest(header.Id, payload, size - 1, request));
        Check(!DecodeRequest(header.Id, payload, size + 1, request));
        Check(DecodeRequest(header.Id, payload, size, request));
        ValidationContext context;
        context.id = header.Id;
        context.payload = payload;
        context.size = size;
        Check(PacketValidator::Envelope(context) == Error::Unauthenticated);
        context.authenticated = true;
        Check(PacketValidator::Envelope(context) == Error::None);
        Check(PacketValidator::Contract(ValidationContext::ForRequest(request)) == Error::None);
        Check(request.requestId == 4294967300ULL);
        Check(request.epoch == 102);
        Check(request.revision == 103);
        ++count;
    }
    Event event;
    event.requestId = 4294967300ULL;
    event.actionId = 4294967301ULL;
    event.epoch = 102;
    event.revision = 103;
    event.sequence = 104;
    event.entity = 105;
    event.target = 106;
    event.definition = 107;
    event.position = {1.25f, -2.5f, 3.75f};
    event.velocity = {1.25f, -2.5f, 3.75f};
    event.serverTime = 12345.5;
    event.hp = 11.25f;
    event.maxHp = 12.25f;
    event.radius = 13.25f;
    event.duration = 14.25f;
    event.hitTime = 15.25f;
    event.value = 16.25f;
    event.code = 117;
    event.flags = 118;
    event.mode = Mode::Combat;
    event.error = Error::Cooldown;
    event.kind = EventKind::Ack;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 62);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1793);
        std::ofstream output(folder + "S2C_ACK.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Welcome;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 74);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1794);
        std::ofstream output(folder + "S2C_WELCOME.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Spawn;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 90);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1795);
        std::ofstream output(folder + "S2C_SPAWN.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Despawn;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 28);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1796);
        std::ofstream output(folder + "S2C_DESPAWN.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::State;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 90);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1797);
        std::ofstream output(folder + "S2C_STATE.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::MoveResult;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 80);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1798);
        std::ofstream output(folder + "S2C_MOVE_RESULT.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::ActionStarted;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 108);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1799);
        std::ofstream output(folder + "S2C_ACTION_STARTED.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::ActionEnded;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 108);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1800);
        std::ofstream output(folder + "S2C_ACTION_ENDED.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::ActionCancelled;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 108);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1801);
        std::ofstream output(folder + "S2C_ACTION_CANCELLED.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Hit;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 104);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1802);
        std::ofstream output(folder + "S2C_HIT.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Inventory;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 36);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1803);
        std::ofstream output(folder + "S2C_INVENTORY.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Zone;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 34);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1804);
        std::ofstream output(folder + "S2C_ZONE.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::SnapshotEnd;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 28);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1805);
        std::ofstream output(folder + "S2C_SNAPSHOT_END.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Stats;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 82);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1806);
        std::ofstream output(folder + "S2C_STATS.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Equipment;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 34);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1807);
        std::ofstream output(folder + "S2C_EQUIPMENT.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Quest;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 40);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1808);
        std::ofstream output(folder + "S2C_QUEST.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Tutorial;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 34);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1809);
        std::ofstream output(folder + "S2C_TUTORIAL.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Saved;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 40);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1810);
        std::ofstream output(folder + "S2C_SAVED.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::Growth;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 58);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1811);
        std::ofstream output(folder + "S2C_GROWTH.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    event.kind = EventKind::SkillRank;
    {
        const auto bytes = EncodeEvent(event);
        Check(bytes.size() == 46);
        Check(static_cast<uint8_t>(bytes[2]) + 256 * static_cast<uint8_t>(bytes[3]) == 1812);
        std::ofstream output(folder + "S2C_SKILL_RANK.bin", std::ios::binary);
        output.write(bytes.data(), bytes.size());
    }
    ++count;
    Request request;
    Check(!DecodeRequest(0x500, "unused", 6, request));
    Check(!DecodeRequest(0xFFFF, "unused", 6, request));
    Check(!DecodeRequest(1537, nullptr, 16, request));
    std::cout << "PASS: " << count << " cross-language packet layouts; size/authentication/count/legacy-ID rejection" << std::endl;
}
