#pragma once
#include "Util/Module.hpp"
#include "Util/Util.hpp"


class JsonArchive;

class ModuleGameInfo : public IModule, public IMessageReceiver, public IStateHolder {
    std::string worldName;


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
