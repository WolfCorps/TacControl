#pragma once
#include <string_view>
#include <vector>


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
    virtual void OnNetMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {}
    virtual std::string_view GetMessageReceiverName() = 0;
};



#define MODULES_LIST(X)\
    X(NetworkController) \
    