#pragma once
#include "Util/Module.hpp"
#include "Util/Util.hpp"


class JsonArchive;

class ModuleGameInfo : public IModule, public IMessageReceiver, public IStateHolder {
public: //#TODO private and getter
    std::string worldName;
    // local player Direct Play ID
    uint64_t playerDPId = 0;
    // list of all players in MP with their Direct Play ID
    std::map<uint64_t, std::string> playerList;
public:

    //IModule
    void ModuleInit() override;


    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "GameInfo"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    //void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "GameInfo"sv; };
    void SerializeState(JsonArchive& ar) override;
   
};

inline ModuleGameInfo GModuleGameInfo;
