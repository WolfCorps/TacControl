#pragma once
#include "Module.hpp"
#include <chrono>
#include <functional>
#include <future>
#include <queue>
#include <mutex>

using namespace std::chrono_literals;

class Thread : public IModule {
protected:
    std::unique_ptr<std::thread> myThread;

    bool shouldRun = true;//don't need atomic here. believe me.

public:
    void Init() override;
    virtual void Run() = 0;
    virtual void StopThread() { shouldRun = false; };
};

class ThreadTask {
public:
    std::function<void()> job;
    std::promise<void> prom;
};



class ThreadQueue : public Thread {
protected:
    std::queue<ThreadTask> taskQueue;

    std::condition_variable threadWorkCondition;
    std::mutex taskQueueMutex;


    void Run() override;
    void StopThread() override;

public:
    std::future<void> AddTask(std::function<void()>&& task);

};

class ThreadQueuePeriodic final : public ThreadQueue {
protected:
    struct PeriodicTask {
        std::chrono::system_clock::time_point nextExecute;
        std::chrono::milliseconds interval;
        std::function<void()> task;
        std::string taskName;
    };

    std::vector<PeriodicTask> periodicTasks;
    std::mutex periodicTasksMutex;
    std::chrono::milliseconds periodicDuration = 1000ms;


    void Run() override;

public:
    void AddPeriodicTask(std::string_view taskName, std::chrono::milliseconds interval, std::function<void()>&& task);
    void RemovePeriodicTask(std::string_view taskName);

};
