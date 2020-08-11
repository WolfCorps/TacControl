#include "ModuleMarker.hpp"

#include "Game/GameManager.hpp"

void ModuleMarker::OnGameMessage(const std::vector<std::string_view>& function,
                                 const std::vector<std::string_view>& arguments) {

    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "MarkerTypes") {
        //OnRadioUpdate(arguments);
    }








}

void ModuleMarker::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments) {
    
}

void ModuleMarker::SerializeState(JsonArchive& ar) {
    
}

void ModuleMarker::OnGamePostInit() {
    GGameManager.SendMessage("Marker.Cmd.GetMarkerTypes", "");
}
