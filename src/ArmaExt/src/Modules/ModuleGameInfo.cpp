#include "ModuleGameInfo.hpp"


#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"
void ModuleGameInfo::ModuleInit() {}

void ModuleGameInfo::OnGameMessage(const std::vector<std::string_view>& function,
    const std::vector<std::string_view>& arguments) {

    if (function[0] == "WorldLoaded") {
        worldName = arguments[0];
        GNetworkController.SendStateUpdate();
    }




}

void ModuleGameInfo::SerializeState(JsonArchive& ar) {
    ar.Serialize("worldName", worldName);
}
