#pragma once
#include "Util/Module.hpp"
#include "Util/Util.hpp"


class websocket_session;
class JsonArchive;

class ModuleStreamManager : public IModule, public IMessageReceiver {

    std::unordered_map<std::string, std::vector<std::weak_ptr<websocket_session>>, Util::string_hash, std::equal_to<>> streamSubscribers;

    void OnInterestChanged(std::string_view interestName, std::shared_ptr<websocket_session> session, bool isInterested);

public:
    //IModule
    void ModuleInit() override;

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "StreamMng"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments,
        const NetworkMessageContext& context) override;
};

inline ModuleStreamManager GModuleStreamManager;
