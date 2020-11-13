#pragma once
#include "Util/Module.hpp"
#include "Util/Util.hpp"


class JsonArchive;

class ModuleACE : public IModule, public IMessageReceiver, public IStateHolder {
public: 
    struct Explosive {
        std::string netId;
        uint16_t explosiveCode;
        std::string explosiveClass;
        std::string detonator;

        void Serialize(JsonArchive& ar);
    };

    std::vector<Explosive> explosives;


public:
    //IModule
    void ModuleInit() override;

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "ACE"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments,
        const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "ACE"sv; };
    void SerializeState(JsonArchive& ar) override;

    
};

inline ModuleACE GModuleACE;
