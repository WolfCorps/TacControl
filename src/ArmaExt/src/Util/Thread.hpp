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
    ~Thread();
    void ModuleInit() override;
    virtual void Run() = 0;
    virtual void StopThread() { shouldRun = false; }
    void SetThreadName(std::string name);
};

#include <type_traits>

//https://stackoverflow.com/questions/30854070/return-values-for-active-objects/30854998#30854998
template<class Sig>
struct task;
namespace details_task {
    template<class Sig>
    struct ipimpl;
    template<class R, class...Args>
    struct ipimpl<R(Args...)> {
        virtual ~ipimpl() {}
        virtual R invoke(Args&&...args) const = 0;
    };
    template<class Sig, class F>
    struct pimpl;
    template<class R, class...Args, class F>
    struct pimpl<R(Args...), F> :ipimpl<R(Args...)> {
        F f;
        R invoke(Args&&...args) const final override {
            return f(std::forward<Args>(args)...);
        };
    };
    // void case, we don't care about what f returns:
    template<class...Args, class F>
    struct pimpl<void(Args...), F> :ipimpl<void(Args...)> {
        F f;
        template<class Fin>
        pimpl(Fin&& fin) :f(std::forward<Fin>(fin)) {}
        void invoke(Args&&...args) const final override {
            f(std::forward<Args>(args)...);
        };
    };
}

template<class R, class...Args>
struct task<R(Args...)> {
    std::unique_ptr< details_task::ipimpl<R(Args...)> > pimpl;
    task(task&&) = default;
    task& operator=(task&&) = default;
    task() = default;
    explicit operator bool() const { return static_cast<bool>(pimpl); }

    R operator()(Args...args) const {
        return pimpl->invoke(std::forward<Args>(args)...);
    }
    // if we can be called with the signature, use this:
    template<class F, class = std::enable_if_t <
        std::is_convertible<std::invoke_result_t<F const& (Args...)>, R>{}
    >>
        task(F&& f) :task(std::forward<F>(f), std::is_convertible<F&, bool>{}) {}

    // the case where we are a void return type, we don't
    // care what the return type of F is, just that we can call it:
    template<class F, class R2 = R, class = std::invoke_result_t<F const& (Args...)>,
        class = std::enable_if_t < std::is_same<R2, void>{} >
    >
        task(F&& f) :task(std::forward<F>(f), std::is_convertible<F&, bool>{}) {}

    // this helps with overload resolution in some cases:
    task(R(*pf)(Args...)) :task(pf, std::true_type{}) {}
    // = nullptr support:
    task(std::nullptr_t) :task() {}

private:
    // build a pimpl from F.  All ctors get here, or to task() eventually:
    template<class F>
    task(F&& f, std::false_type /* needs a test?  No! */) :
        pimpl(new details_task::pimpl<R(Args...), std::decay_t<F>>{ std::forward<F>(f) })
    {}
    // cast incoming to bool, if it works, construct, otherwise
    // we should be empty:
    // move-constructs, because we need to run-time dispatch between two ctors.
    // if we pass the test, dispatch to task(?, false_type) (no test needed)
    // if we fail the test, dispatch to task() (empty task).
    template<class F>
    task(F&& f, std::true_type /* needs a test?  Yes! */) :
        task(f ? task(std::forward<F>(f), std::false_type{}) : task())
    {}
};




class ThreadTask {
public:
    task<void()> job;
    std::promise<void> prom;
};



class ThreadQueue : public Thread {
protected:
    std::queue<ThreadTask> taskQueue;

    std::condition_variable threadWorkCondition;
    std::mutex taskQueueMutex;

    ~ThreadQueue();
    void Run() override;
    void StopThread() override;
public:
    std::future<void> AddTask(task<void()> task);

};

class ThreadQueuePeriodic : public ThreadQueue {
protected:
    struct PeriodicTask {
        std::chrono::system_clock::time_point nextExecute;
        std::chrono::milliseconds interval;
        std::function<void()> task;
        std::string taskName;
    };

    std::vector<PeriodicTask> periodicTasks;
    std::recursive_mutex periodicTasksMutex;
    std::chrono::milliseconds periodicDuration = 1000ms;


    void Run() override;

public:
    void AddPeriodicTask(std::string_view taskName, std::chrono::milliseconds interval, std::function<void()>&& task);
    void RemovePeriodicTask(std::string_view taskName);

};
