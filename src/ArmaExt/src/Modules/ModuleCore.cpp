#include "ModuleCore.hpp"

#include "Game/GameManager.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"

std::vector<tcp::socket::endpoint_type>& ModuleCore::GetSubscriberList(std::string_view name)
{
    auto found = componentSubscribers.find(name);
    if (found == componentSubscribers.end())
    {
        found = componentSubscribers.emplace(name, decltype(componentSubscribers)::mapped_type{}).first;
    }

    return found->second;
}

void ModuleCore::OnInterestLost(std::string_view interest)
{
    GGameManager.SendMessage("Core.UpdateInterest", fmt::format("{}\n{}", interest, false));
}

void ModuleCore::OnInterestGained(std::string_view interest)
{
    GGameManager.SendMessage("Core.UpdateInterest", fmt::format("{}\n{}", interest, true));
}

void ModuleCore::ModuleInit()
{
}

void ModuleCore::OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments)
{

}

void ModuleCore::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const NetworkMessageContext& context)
{
    if (function[0] == "Register") {
        for (auto& it : arguments["interests"]) {

            auto& subscriberList = GetSubscriberList(it);
            if (subscriberList.empty())
                OnInterestGained(it); // Won't be empty after this

            subscriberList.emplace_back(context.sender->GetRemoteEndpoint());
            OnInterestChanged(it, context.sender, true);
            //std::ranges::sort(subscriberList); // For binary find, but not worth it now
        }
    } else if (function[0] == "Unregister") {
        for (auto& it : arguments["interests"]) {

            auto& subscriberList = GetSubscriberList(it);
            std::erase(subscriberList, context.sender->GetRemoteEndpoint());

            if (subscriberList.empty())
                OnInterestLost(it);
            OnInterestChanged(it, context.sender, false);
        }
    }
    SendStateUpdate(); //#TODO optimize, only if Gained/Lost happened
}

void ModuleCore::SerializeState(JsonArchive& ar)
{
    std::vector<std::string> activeComponents; //#TODO this is inefficient, this is only a temporary to the array insert in serialization

    for (auto& [interest, subscribers] : componentSubscribers)
    {
        if (!subscribers.empty())
            activeComponents.emplace_back(interest);
    }

    ar.Serialize("activeInterests", activeComponents);
}

void ModuleCore::OnNetClientJoined(std::shared_ptr<websocket_session>)
{
    // Don't actually care about this :harold:
}

void ModuleCore::OnNetClientLeft(tcp::socket::endpoint_type endpoint)
{
    bool stateChanged = false;
    for (auto& [interest, subscribers] : componentSubscribers)
    {
        if (std::erase(subscribers, endpoint) && subscribers.empty()) // We did erase some elements, and the list is now empty
        {
            stateChanged = true;
            // Can only deactivate here
            OnInterestLost(interest);
        }
    }
    if (stateChanged)
        SendStateUpdate();
}

void ModuleCore::OnGamePostInit()
{
    // Notify game about all currently active interests
    for (auto& [interest, subscribers] : componentSubscribers)
        if (!subscribers.empty())
            OnInterestGained(interest);
}
