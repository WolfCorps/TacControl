#pragma once
#include "Networking/websocket.hpp"
#include "Util/Module.hpp"
#include "Util/Util.hpp"

class JsonArchive;


/**
 * \brief Core Module Provides:
 * - Interest System
 *      Connected clients can specify which components they are interested in.
 *      If no clients are interested in a component the ingame code can disable processing for it and save performance.
 *      So we only pay for what is actually used.
 */
class ModuleCore : public IModule, public IMessageReceiver, public IStateHolder, public INetworkingEventsReceiver, public IPostInitReceiver {

    std::unordered_map<std::string, std::vector<tcp::socket::endpoint_type>, Util::string_hash, std::equal_to<>> componentSubscribers;

    std::vector<tcp::socket::endpoint_type>& GetSubscriberList(std::string_view name);
    void OnInterestLost(std::string_view interest);
    void OnInterestGained(std::string_view interest);

public:
    //IModule
    void ModuleInit() override;

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Core"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments,
        const NetworkMessageContext& context) override;

    //IStateHolder
    std::string_view GetStateHolderName() const override { return "Core"sv; };
    void SerializeState(JsonArchive& ar) override;

    // INetworkingEventsReceiver
    void OnNetClientJoined(std::shared_ptr<websocket_session>) override;
    void OnNetClientLeft(tcp::socket::endpoint_type) override;

    //IPostInitReceiver
    void OnGamePostInit() override;
};

inline ModuleCore GModuleCore;
