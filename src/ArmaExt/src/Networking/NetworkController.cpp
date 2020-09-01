#include "NetworkController.hpp"


#include "Game/GameManager.hpp"
#include "Serialize.hpp"
#include "Util/Logger.hpp"
#include "websocket.hpp"

void NetworkController::ModuleInit() {

    wsServer = new Server();
    wsServer->state_->OnMessage.connect([this](const std::string& message, boost::shared_ptr<websocket_session> sender) {
        Logger::log(LoggerTypes::General, message);


        auto replyFunc = [sender](std::string_view message) {
            auto const ss = boost::make_shared<std::string const>(message);
            sender->send(ss);
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
        JsonArchive state;

        GGameManager.CollectGameState(state);

        wsServer->state_->updateState(*(state.getRaw()));
    }); 
}
