// TEST ONLY. No account authentication. Never deploy; binds 127.0.0.1 only.
// Runs the exact production gameplay Service behind a test TCP adapter when MySQL is unavailable.
#define WIN32_LEAN_AND_MEAN
#include "Gameplay/Protocol.h"
#include "Gameplay/Service.h"
#include "Link.h"
#include "../../EverWindServer/LoginServer/Network/Packet.h"
#include <atomic>
#include <iostream>
#include <mutex>
#include <thread>
#include <vector>
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "Ws2_32.lib")
using namespace EverWind::Gameplay;

int main(int argc, char **argv)
{
    if (argc < 2)
        return 2;
    WSADATA data{};
    WSAStartup(MAKEWORD(2, 2), &data);
    Service service; service.InitializeGame(argv[1]); service.InitializeMaps(); service.InitializeWorldObjects(); service.Start();
    const float moveSpeed = argc > 2 ? std::stof(argv[2]) : 5.0f;
    SOCKET listener = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    sockaddr_in address{};
    address.sin_family = AF_INET;
    address.sin_port = htons(14000);
    inet_pton(AF_INET, "127.0.0.1", &address.sin_addr);
    if (bind(listener, reinterpret_cast<sockaddr *>(&address), sizeof address) || listen(listener, 8))
    {
        std::cerr << "Cannot bind test port\n";
        return 1;
    }
    std::cout << "TEST ONLY loopback host 127.0.0.1:14000; no production authentication\n" << std::flush;
    int next = 1;
    std::vector<std::thread> threads;
    for (;;)
    {
        SOCKET s = accept(listener, nullptr, nullptr);
        if (s == INVALID_SOCKET)
            break;
        auto link = std::make_shared<Link>();
        link->socket = s;
        int id = next++;
        threads.emplace_back([link, id, moveSpeed, &service] {
            bool authenticated = false;
            Header h{};
            while (link->Read(reinterpret_cast<char *>(&h), sizeof h))
            {
                if (h.length < 4 || h.length > 1024)
                    break;
                std::vector<char> body(h.length - 4);
                if (!link->Read(body.data(), static_cast<int>(body.size())))
                    break;
                if (h.id == 0x100 && body.size() == 96 && !authenticated)
                {
                    authenticated = true;

                    link->SendBytes(Encode(0x101, NetPackets::PKT_LOGIN_ACK{0, id, 1, 0, 0, -10, 0}));
                }
                else if (authenticated && h.id == RequestPacket && body.size() == sizeof(Request))
                {
                    Request r;
                    std::memcpy(&r, body.data(), sizeof r);
                    std::weak_ptr<Link> weak = link;
                    service.Submit(
                        id, r,
                        [weak](const Event &e) {
                            if (auto c = weak.lock())
                                c->SendBytes(Encode(EventPacket, e));
                        },
                        [weak] {
                            auto c = weak.lock();
                            return c && c->alive.load();
                        }, moveSpeed);
                }
                else
                    break;
            }
            link->alive = false;
            shutdown(link->socket, SD_BOTH);
        });
    }
    service.Stop();
    for (auto &t : threads)
        if (t.joinable())
            t.join();
    closesocket(listener);
    WSACleanup();
}
