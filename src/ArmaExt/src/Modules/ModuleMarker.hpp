#pragma once
#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;


class ModuleMarker : public ThreadQueue, public IMessageReceiver, public IStateHolder, IPostInitReceiver {


public:





    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Marker"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "Marker"sv; };
    void SerializeState(JsonArchive& ar) override;

    //IPostInitReceiver
    void OnGamePostInit() override;
};

inline ModuleMarker GModuleMarker;
