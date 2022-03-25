#include "NetworkController.hpp"


#include "Game/GameManager.hpp"
#include "Serialize.hpp"
#include "Util/Logger.hpp"
#include "websocket.hpp"

template<class> inline constexpr bool always_false_v = false;

void NetworkController::ModuleInit() {

    wsServer = new Server();
    wsServer->state_->OnMessage.connect([this](const std::string& message, boost::shared_ptr<websocket_session> sender) {
        Logger::log(LoggerTypes::General, message);


        auto replyFunc = [sender](ReplyMessageType message) {


            std::visit([&sender](auto&& arg) {
                using T = std::decay_t<decltype(arg)>;
                if constexpr (std::is_same_v<T, std::string_view>) {
                    auto const ss = boost::make_shared<websocket_session::MessageType>(std::string(arg));
                    sender->send(ss);
                }
                else if constexpr (std::is_same_v<T, std::reference_wrapper<nlohmann::json>>) {
                    sender->send(arg.get());
                }
                else
                    static_assert(always_false_v<T>, "non-exhaustive visitor!");
                }, message);
        };

        try {
            auto msg = nlohmann::json::parse(message);

            if (msg.contains("cmd")) {
                std::vector<std::string_view> command;
                for (auto& it : msg["cmd"])
                    command.emplace_back(it);

                GGameManager.TransferNetworkMessage(std::move(command), std::move(msg["args"]), replyFunc);
            }
        } catch (...) {
            Util::BreakToDebuggerIfPresent();
            return;
        }
    });
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
