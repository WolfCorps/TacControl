#include "NetworkController.hpp"


#include "Game/GameManager.hpp"
#include "Serialize.hpp"
#include "Util/Logger.hpp"
#include "websocket.hpp"
#include "Util/AllModuleHeaders.hpp"

template<class> inline constexpr bool always_false_v = false;

void NetworkController::ModuleInit() {

    wsServer = new Server();
    wsServer->state_->OnMessage.connect([this](const std::string& message, std::shared_ptr<websocket_session> sender) {
        Logger::log(LoggerTypes::General, message);

        try {
            auto msg = nlohmann::json::parse(message);

            if (msg.contains("cmd")) {
                std::vector<std::string_view> command;
                for (auto& it : msg["cmd"])
                    command.emplace_back(it);

                GGameManager.TransferNetworkMessage(std::move(command), std::move(msg["args"]), sender);
            }
        } catch (...) {
            Util::BreakToDebuggerIfPresent();
            return;
        }
    });


    wsServer->state_->OnClientJoined.connect([](std::shared_ptr<websocket_session> sender) {
        Logger::log(LoggerTypes::General, fmt::format("New Client {}:{}", sender->GetRemoteEndpoint().address().to_string(), sender->GetRemoteEndpoint().port()));
    });

    wsServer->state_->OnClientLeft.connect([](tcp::socket::endpoint_type oldClient) {
        Logger::log(LoggerTypes::General, fmt::format("Client Disconnected {}:{}", oldClient.address().to_string(), oldClient.port()));
    });

    auto RegisterJoinForwarder = [this]<typename ModuleType>(ModuleType& module) {
        if constexpr (std::is_base_of_v<INetworkingEventsReceiver, ModuleType>) {
            wsServer->state_->OnClientJoined.connect([&module](std::shared_ptr<websocket_session> newClient) {
                try {
                    module.OnNetClientJoined(newClient);
                }
                catch (...) {
                    Util::BreakToDebuggerIfPresent();
                    return;
                }
            });

            wsServer->state_->OnClientLeft.connect([&module](tcp::socket::endpoint_type oldClient) {
                try {
                    module.OnNetClientLeft(oldClient);
                }
                catch (...) {
                    Util::BreakToDebuggerIfPresent();
                    return;
                }
            });
        }
    };

#define MODULES_REGJOINFWD(x) \
    RegisterJoinForwarder((G##x));

    MODULES_LIST(MODULES_REGJOINFWD);

    ThreadQueue::ModuleInit();
}


void NetworkController::SendStateUpdate() {
    AddTask([this]() {
        JsonArchive state(currentState, false); // We are serializing INTO currentState

        GGameManager.CollectGameState(state);

        wsServer->state_->updateState(currentState);
    }); 
}

void NetworkController::SendStateUpdate(std::string_view subset) {
    AddTask([this, subset]() {
        JsonArchive state(currentState, false); // We are serializing INTO currentState

        GGameManager.CollectGameState(state, subset);

        wsServer->state_->updateState(currentState);
    });
}


void IStateHolder::SendStateUpdate() const {
    GNetworkController.SendStateUpdate(GetStateHolderName());
}

void NetworkMessageContext::Reply(ReplyMessageType reply) const {
    std::visit([this](auto&& arg) {
        using T = std::decay_t<decltype(arg)>;
        if constexpr (std::is_same_v<T, std::string_view>) {
            auto const ss = std::make_shared<websocket_session::MessageType>(std::string(arg));
            sender->send(ss);
        }
        else if constexpr (std::is_same_v<T, std::reference_wrapper<nlohmann::json>>) {
            sender->send(arg.get());
        }
        else
            static_assert(always_false_v<T>, "non-exhaustive visitor!");
        }, reply);
}

