#include "TimerManager.h"
#include "Timer.h"

TimerManager::TimerManager()
    : running(false)
{
}

TimerManager::~TimerManager()
{
    Stop();
}

void TimerManager::AddOnce(std::chrono::steady_clock::duration delay, std::function<void()> callback)
{
    auto executeTime = std::chrono::steady_clock::now() + delay;
    auto timer = std::make_shared<Timer>(executeTime, callback, false, std::chrono::steady_clock::duration::zero());

    {
        std::lock_guard<std::mutex> lock(timeMutex);
        timerQueue.push(timer);
    }

    cv.notify_one();
}

void TimerManager::AddRepeat(std::chrono::steady_clock::duration interval, std::function<void()> callback)
{
    auto executeTime = std::chrono::steady_clock::now() + interval;
    auto timer = std::make_shared<Timer>(executeTime, callback, true, interval);

    {
        std::lock_guard<std::mutex> lock(timeMutex);
        timerQueue.push(timer);
    }

    cv.notify_one();
}

void TimerManager::Start()
{
    if (running.exchange(true))
    {
        return;
    }

    timerThread = std::thread(&TimerManager::Run, this);
}

void TimerManager::Stop()
{
    if (!running.exchange(false))
    {
        return;
    }

    cv.notify_one();

    if (timerThread.joinable())
    {
        timerThread.join();
    }
}

bool TimerManager::TimerCompare::operator()(const std::shared_ptr<Timer>& lhs, const std::shared_ptr<Timer>& rhs) const
{
    return lhs->GetNextExecuteTime() > rhs->GetNextExecuteTime();
}

void TimerManager::Run()
{
    while (running)
    {
        std::shared_ptr<Timer> currentTimer;

        {
            std::unique_lock<std::mutex> lock(timeMutex);

            cv.wait(lock, [this]()
            {
                return !running || !timerQueue.empty();
            });

            if (!running)
            {
                break;
            }

            currentTimer = timerQueue.top();
            auto nextTime = currentTimer->GetNextExecuteTime();

            if (std::chrono::steady_clock::now() < nextTime)
            {
                cv.wait_until(lock, nextTime);

                if (!running)
                {
                    break;
                }

                if (timerQueue.empty())
                {
                    continue;
                }

                currentTimer = timerQueue.top();

                if (std::chrono::steady_clock::now() < currentTimer->GetNextExecuteTime())
                {
                    continue;
                }
            }

            timerQueue.pop();
        }

        currentTimer->Execute();

        if (currentTimer->IsRepeat() && running)
        {
            currentTimer->UpdateNextExecuteTime();

            std::lock_guard<std::mutex> lock(timeMutex);
            timerQueue.push(currentTimer);
        }
    }
}
