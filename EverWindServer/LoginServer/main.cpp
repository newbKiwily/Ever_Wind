#include "ServerApplication.h"
#include <atomic>
#include <csignal>
#include <iostream>
namespace
{
std::atomic<bool> shouldStop = false;
void SignalHandler(int)
{
    shouldStop.store(true);
}
} // namespace
int main()
{
    signal(SIGINT, SignalHandler);
    signal(SIGTERM, SignalHandler);
    EverWind::ServerApplication application;
    EverWind::ServerConfig config;
    if (!application.Initialize(config) || !application.Start())
        return -1;
    std::cout << "Login Server started on port " << config.port << ". Type 'quit' to stop." << std::endl;
    std::string command;
    while (!shouldStop.load() && std::getline(std::cin, command))
        if (command == "quit" || command == "exit")
            break;
    application.Shutdown();
    std::cout << "Server stopped." << std::endl;
    return 0;
}
