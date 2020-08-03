#pragma once
#include "Util/Thread.hpp"

using namespace std::string_view_literals;

class NetworkController final : public ThreadQueue, public IMessageReceiver {

public:
    void Init() override;
    std::string_view GetMessageReceiverName() override { return "NetworkController"sv; }
};


static inline NetworkController GNetworkController;