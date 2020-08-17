#include "Thread.hpp"

void Thread::ModuleInit() {
    myThread = std::make_unique<std::thread>(&Thread::Run, this);
}

//http://msdn.microsoft.com/en-us/library/xcb2z8hs(VS.90).aspx

//
// Usage: SetThreadName (-1, "MainThread");
//
#include <windows.h>
const DWORD MS_VC_EXCEPTION = 0x406D1388;

#pragma pack(push,8)
typedef struct tagTHREADNAME_INFO
{
    DWORD dwType; // Must be 0x1000.
    LPCSTR szName; // Pointer to name (in user addr space).
    DWORD dwThreadID; // Thread ID (-1=caller thread).
    DWORD dwFlags; // Reserved for future use, must be zero.
} THREADNAME_INFO;
#pragma pack(pop)

void SetThreadName(DWORD dwThreadID, const char* threadName)
{
    THREADNAME_INFO info;
    info.dwType = 0x1000;
    info.szName = threadName;
    info.dwThreadID = dwThreadID;
    info.dwFlags = 0;

    __try
    {
        RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR), (ULONG_PTR*)&info);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
    }
}


void Thread::SetThreadName(std::string name) {
    DWORD threadId = ::GetThreadId(static_cast<HANDLE>(myThread->native_handle()));
    ::SetThreadName(threadId, name.data());
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
