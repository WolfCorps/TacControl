#pragma once

#include <span>
#include <string_view>
#include <unordered_map>
#include <vector>

#include <json.hpp>
#include "Util/Util.hpp"
#include "GameMessage.hpp"
#include "Util/Thread.hpp"

class JsonArchive;
class IMessageReceiver;

extern "C"
{
    __declspec(dllexport) void __stdcall RVExtension(char* output, int outputSize, const char* function);
    __declspec(dllexport) int __stdcall RVExtensionArgs(char* output, int outputSize, const char* function, const char** argv, int argc);
    __declspec(dllexport) void __stdcall RVExtensionVersion(char* output, int outputSize);
    __declspec(dllexport) void RVExtensionRegisterCallback(int(*callbackProc)(char const* name, char const* function, char const* data));
}

//Handles all Extension interaction with Arma itself
class GameManager {
    friend __declspec(dllexport) void RVExtensionRegisterCallback(int(*callbackProc)(char const* name, char const* function, char const* data));
    static int(*extensionCallback)(char const* name, char const* function, char const* data);

    friend __declspec(dllexport) int RVExtensionArgs(char* output, int outputSize, const char* function, const char** argv, int argc);
    void IncomingMessage(std::unique_ptr<GameMessage> message);

    friend __declspec(dllexport) void RVExtension(char* output, int outputSize, const char* function);


    std::unordered_map<std::string, std::pair<IMessageReceiver*, ThreadQueue*>, Util::string_hash, std::equal_to<>> messageReceiverLookup;


public:


    void SendMessage(std::string_view function, std::string_view arguments);
    void SendMessageInternal(std::string_view function, const std::vector<std::string_view>& arguments);
    void TransferNetworkMessage(std::vector<std::string_view>&& function, nlohmann::json&& arguments, const NetworkMessageContext& context);

    void CollectGameState(JsonArchive& ar);
    void CollectGameState(JsonArchive& ar, std::string_view subset);
};


inline GameManager GGameManager;
