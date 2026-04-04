#pragma once

#include <functional>
#include <chrono>

class Timer
{
public:
    Timer(
        std::chrono::steady_clock::time_point nextExecuteTime,
        std::function<void()> callback,
        bool repeat,
        std::chrono::steady_clock::duration interval
    );

    std::chrono::steady_clock::time_point GetNextExecuteTime() const;
    bool IsRepeat() const;

    void Execute() const;
    void UpdateNextExecuteTime();

private:
    std::chrono::steady_clock::time_point nextExecuteTime_;
    std::chrono::steady_clock::duration interval_;
    std::function<void()> callback_;
    bool repeat_;
};
