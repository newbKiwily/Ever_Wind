#include "Packet.h"
namespace NetPackets
{
void ValidatePacketLength(uint16_t len)
    {
        if (len < MIN_PACKET_SIZE || len > MAX_PACKET_SIZE)
        {
            throw std::runtime_error("Invalid packet length");
        }
    }
}
