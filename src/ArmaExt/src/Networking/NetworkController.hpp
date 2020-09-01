#pragma once
#include "Util/Thread.hpp"

class Server;
using namespace std::string_view_literals;

class NetworkController final : public ThreadQueue, public IMessageReceiver {
    Server* wsServer; //unique_ptr doesn't work cuz forward decl is not enough as it has to know how to delete

    nlohmann::json currentState;
public:
    void ModuleInit() override;
    std::string_view GetMessageReceiverName() override { return "NetworkController"sv; }

    void SendStateUpdate();
    void SendStateUpdate(std::string_view subset);
};


inline NetworkController GNetworkController;
