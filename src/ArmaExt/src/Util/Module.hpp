#pragma once
#include <span>
#include <string_view>
#include <vector>

#include "nlohmann/json.hpp"

using namespace std::string_view_literals;

class JsonArchive;

class IModule {
public:
    virtual ~IModule() = default;
    virtual void ModulePreInit() {}
    virtual void ModuleInit() = 0;
    virtual void ModulePostInit() {}
};

class IMessageReceiver {
public:
    virtual ~IMessageReceiver() = default;
    virtual void OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {}
    virtual void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {}
    virtual std::string_view GetMessageReceiverName() = 0;
};

class IStateHolder {
public:
    virtual std::string_view GetStateHolderName() = 0;
    virtual void SerializeState(JsonArchive& ar) = 0;
};

class IPreInitReceiver {
public:
    virtual void OnGamePreInit() = 0;
};

class IPostInitReceiver {
public:
    virtual void OnGamePostInit() = 0;
};


#define MODULES_LIST(X)\
    X(NetworkController) \
    X(RadioModule) \
    X(ModuleLogitechG15) \
    X(ModuleGPS) \
    X(ModuleMarker) \
    X(ModuleImageDirectory) \
    X(ModuleGameInfo) \
