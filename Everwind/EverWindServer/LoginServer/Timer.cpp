#include "Timer.h"

Timer::Timer(std::chrono::steady_clock::time_point nextExecuteTime,std::function<void()> callback,bool repeat,std::chrono::steady_clock::duration interval)
    : nextExecuteTime_(nextExecuteTime),interval_(interval),callback_(callback),repeat_(repeat)
{
}

std::chrono::steady_clock::time_point Timer::GetNextExecuteTime() const
{
    return nextExecuteTime_;
}

bool Timer::IsRepeat() const
{
    return repeat_;
}

void Timer::Execute() const
{
    if (callback_)
    {
        callback_();
    }
}

void Timer::UpdateNextExecuteTime()
{
    nextExecuteTime_ += interval_;
}
