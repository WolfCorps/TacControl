#include "ModuleGameInfo.hpp"


#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"
void ModuleGameInfo::ModuleInit() {}

void ModuleGameInfo::OnGameMessage(const std::vector<std::string_view>& function,
    const std::vector<std::string_view>& arguments) {

    if (function[0] == "WorldLoaded") {
        worldName = arguments[0];
        GNetworkController.SendStateUpdate(GetStateHolderName());
    } else if (function[0] == "PlayerId") {
        playerDPId = Util::parseArmaNumberToInt64(arguments[0]);
        GNetworkController.SendStateUpdate(GetStateHolderName());
    } else if (function[0] == "PlayerJoined") {
        auto playerName = std::string(arguments[0]);

        auto playerDPId = Util::parseArmaNumberToInt64(arguments[1]);
        playerList.insert({ playerDPId, std::move(playerName) });

        GNetworkController.SendStateUpdate(GetStateHolderName());
    } else if (function[0] == "PlayerLeft") {
        auto playerDPId = Util::parseArmaNumberToInt64(arguments[0]);

        playerList.erase(playerDPId);

        GNetworkController.SendStateUpdate(GetStateHolderName());
    }

}

void ModuleGameInfo::SerializeState(JsonArchive& ar) {
    ar.Serialize("worldName", worldName);
    ar.Serialize("playerid", playerDPId);

    JsonArchive playerListAr;
    //Want to pass empty object, instead of null
    *playerListAr.getRaw() = nlohmann::json::object();
    for (auto& [key, value] : playerList) {
        playerListAr.Serialize(std::to_string(key).c_str(), value);
    }

    ar.Serialize("players", std::move(playerListAr));
}
