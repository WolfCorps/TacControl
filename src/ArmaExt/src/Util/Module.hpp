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
    virtual void PreInit() {}
    virtual void Init() = 0;
    virtual void PostInit() {}
};

class IMessageReceiver {
public:
    virtual ~IMessageReceiver() = default;
    virtual void OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {}
    virtual void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments) {}
    virtual std::string_view GetMessageReceiverName() = 0;
};

class IStateHolder {
public:
    virtual std::string_view GetStateHolderName() = 0;
    virtual void SerializeState(JsonArchive& ar) = 0;
};



#define MODULES_LIST(X)\
    X(NetworkController) \
    X(RadioModule) \
    X(ModuleLogitechG15) \
    X(ModuleGPS) \
