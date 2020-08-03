#pragma once
#include "Module.hpp"
#include <chrono>
#include <functional>
#include <queue>
#include <mutex>

using namespace std::chrono_literals;

class Thread : public IModule {
protected:
    std::unique_ptr<std::thread> myThread;
public:
    void Init() override;
    virtual void Run() = 0;
    virtual void StopThread() = 0;
};

class ThreadQueue : public Thread {
protected:
    std::queue<std::function<void()>> taskQueue;

    std::condition_variable threadWorkCondition;
    std::mutex taskQueueMutex;

    bool shouldRun = true;//don't need atomic here. believe me.

    void Run() override;
    void StopThread() override;

public:
    void AddTask(std::function<void()>&& task);

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