#pragma once

#include <functional>
#include <chrono>
#include <queue>
#include <vector>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <atomic>
#include <memory>

class Timer;

class TimerManager
{

public:
	std::chrono::steady_clock clock;
	std::chrono::steady_clock::duration duration;
	std::function<void()> callBackFunc;

	TimerManager();
	~TimerManager();

	void AddOnce(std::chrono::steady_clock::duration delay, std::function<void()> callback);
	void AddRepeat(std::chrono::steady_clock::duration interval, std::function<void()> callback);

	void Start();
	void Stop();

private:
	struct TimerCompare
	{
		bool operator()(const std::shared_ptr<Timer>& a, const std::shared_ptr<Timer>& b) const;
	};

	void Run();

	std::priority_queue<std::shared_ptr<Timer>, std::vector<std::shared_ptr<Timer>>, TimerCompare> timerQueue;

	std::thread timerThread;
	std::mutex timeMutex;
	std::condition_variable cv;
	std::atomic<bool> running;
};

