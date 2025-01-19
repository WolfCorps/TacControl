#include "ModuleStreamManager.hpp"
#include "ModuleCore.hpp"

#include "Game/GameManager.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"

template <typename T, typename U>
static bool equals(const std::weak_ptr<T>& t, const std::weak_ptr<U>& u)
{
    return !t.owner_before(u) && !u.owner_before(t);
}


template <typename T, typename U>
static bool equals(const std::weak_ptr<T>& t, const std::shared_ptr<U>& u)
{
    return !t.owner_before(u) && !u.owner_before(t);
}

// Called by networking
void ModuleStreamManager::OnInterestChanged(std::string_view interestName, std::shared_ptr<websocket_session> session, bool isInterested)
{
    if (!interestName.starts_with("S_"))
        return;

    if (isInterested)
    {
        auto found = streamSubscribers.emplace(interestName, std::vector<WebsocketSessionWeak>{}).first;
        found->second.emplace_back(session->weak_from_this());
    }
    else
    {
        auto found = streamSubscribers.find(interestName);
        if (found == streamSubscribers.end())
            return; // No-one was subscribed to this to begin with

        found->second.erase(
            std::ranges::remove_if(found->second, [&session](WebsocketSessionWeak& wptr)
            {
                    return equals(wptr, session) || wptr.expired();
            }).begin(),
            found->second.end()
        );
    }
}

void ModuleStreamManager::ModuleInit()
{
    GModuleCore.OnInterestChanged.connect([this](std::string_view interestName, std::shared_ptr<websocket_session> session, bool isInterested) {
        OnInterestChanged(interestName, std::move(session), isInterested);
    });
}

void ModuleStreamManager::OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments)
{
    if (function[0] == "StreamUpdate") {
        std::string_view streamName = function[1];
        std::string_view streamData = arguments[0];

        // Game will escape quotes..

        try {
            auto msg = nlohmann::json::parse(streamData);


            auto found = streamSubscribers.find(streamName);
            if (found != streamSubscribers.end()) {
                nlohmann::json stateUpdate;
                stateUpdate["cmd"] = "StreamUpdate";
                stateUpdate["s"] = streamName;
                stateUpdate["d"] = streamData;

                for (auto const& wp : found->second)
                    if (auto sp = wp.lock())
                        sp->send(stateUpdate);
            }
        }
        catch (...) {
            Util::BreakToDebuggerIfPresent();
            return;
        }
    }
}

void ModuleStreamManager::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const NetworkMessageContext& context)
{

}
