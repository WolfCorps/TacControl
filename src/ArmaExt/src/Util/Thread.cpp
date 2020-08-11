#include "Thread.hpp"

void Thread::Init() {
    myThread = std::make_unique<std::thread>(&Thread::Run, this);
}

void ThreadQueue::Run() {
    while (shouldRun) {
        std::unique_lock<std::mutex> lock(taskQueueMutex);
        threadWorkCondition.wait(lock, [this] {return !taskQueue.empty() || !shouldRun; });
        if (!shouldRun) return;

        auto task(std::move(taskQueue.front()));
        taskQueue.pop();
        lock.unlock();

        task.job();
        task.prom.set_value();
    }
}

void ThreadQueue::StopThread() {
    if (!myThread)
        return;

    {
        std::lock_guard<std::mutex> lock(taskQueueMutex);
        shouldRun = false;
    }
    threadWorkCondition.notify_one();
    if (myThread->joinable())
        myThread->join();
    myThread = nullptr;
}

std::future<void> ThreadQueue::AddTask(std::function<void()>&& task) {
    std::future<void> fut;
    {
        std::lock_guard<std::mutex> lock(taskQueueMutex);
        ThreadTask newTask;
        newTask.job = std::move(task);

        fut = newTask.prom.get_future();

        taskQueue.emplace(std::move(newTask));
    }
    threadWorkCondition.notify_one();
    return fut;
}

void ThreadQueuePeriodic::Run() {
    while (shouldRun) {
        std::unique_lock<std::mutex> lock(taskQueueMutex);
        threadWorkCondition.wait_for(lock, periodicDuration, [this] {return !taskQueue.empty() || !shouldRun || !periodicTasks.empty(); });
        if (!shouldRun) return;

        if (!taskQueue.empty()) {
            auto task(std::move(taskQueue.front()));
            taskQueue.pop();

            task.job();
            task.prom.set_value();
        }

        auto currentTime = std::chrono::system_clock::now();

        std::unique_lock<std::mutex> lockPeriodic(periodicTasksMutex);
        for (PeriodicTask& it : periodicTasks) {
            if (it.nextExecute < currentTime) {
                it.task();
                it.nextExecute = std::chrono::system_clock::now() + it.interval;
            }
        }
    }
}

void ThreadQueuePeriodic::AddPeriodicTask(std::string_view taskName, std::chrono::milliseconds interval, std::function<void()>&& task) {
    PeriodicTask newTask;
    newTask.taskName = taskName;
    newTask.interval = interval;
    newTask.nextExecute = std::chrono::system_clock::now();
    newTask.task = std::move(task);

    {
        std::unique_lock<std::mutex> lockPeriodic(periodicTasksMutex);
        periodicTasks.emplace_back(std::move(newTask));
    }
}

void ThreadQueuePeriodic::RemovePeriodicTask(std::string_view taskName) {
    std::unique_lock<std::mutex> lockPeriodic(periodicTasksMutex);

    std::erase_if(periodicTasks, [taskName](const PeriodicTask& task) {
            return task.taskName == taskName;
        });

    if (periodicTasks.empty()) {
        lockPeriodic.unlock();
        std::unique_lock<std::mutex> lock(taskQueueMutex);
        periodicDuration = 1000ms;
        return;
    }

    //Recalculate minimum wait interval required to hit the fastest periodic task
    auto minInterval = std::min_element(periodicTasks.begin(), periodicTasks.end(), [](const PeriodicTask& l, const PeriodicTask& r)
        {
            return l.interval < r.interval;
        });

    lockPeriodic.unlock();
    std::unique_lock<std::mutex> lock(taskQueueMutex);
    periodicDuration = minInterval->interval;
}
