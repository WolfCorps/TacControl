#include "ModuleMarker.hpp"

#include "Game/GameManager.hpp"
#include "ModuleImageDirectory.hpp"

void ModuleMarker::OnMarkerTypesRetrieved(const std::vector<std::basic_string_view<char>>& arguments) {

    auto types = Util::split(arguments[0], '\t');
    auto colors = Util::split(arguments[1], '\t');
    auto brushes = Util::split(arguments[2], '\t');

    for (auto& it : types) {
        auto split = Util::split(it, '\n');

        MarkerType type;

        auto classname = split[0];
        type.name = split[1];
        type.color = split[2];
        type.size = Util::parseArmaNumberToInt(split[3]);
        type.icon = split[4];
        type.shadow = Util::parseArmaNumberToInt(split[5]) > 0;

        GModuleImageDirectory.LoadTextureToCache(type.icon);

        markerTypes.emplace(classname, type);
    }

    for (auto& it : colors) {
        auto split = Util::split(it, '\n');

        MarkerColor color;

        auto classname = split[0];
        color.name = split[1];
        color.color = split[2];

        markerColors.emplace(classname, color);
    }


    for (auto& it : brushes) {
        auto split = Util::split(it, '\n');

        MarkerBrush brush;

        auto classname = split[0];
        brush.name = split[1];
        brush.texture = split[2];
        brush.drawBorder = Util::parseArmaNumberToInt(split[3]) > 0;

        markerBrushes.emplace(classname, brush);
    }


   

}

void ModuleMarker::OnGameMessage(const std::vector<std::string_view>& function,
                                 const std::vector<std::string_view>& arguments) {

    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "MarkerTypes") {
        OnMarkerTypesRetrieved(arguments);
    }








}

void ModuleMarker::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {
    
}

void ModuleMarker::SerializeState(JsonArchive& ar) {
    
}

void ModuleMarker::OnGamePostInit() {
    GGameManager.SendMessage("Marker.Cmd.GetMarkerTypes", "");
}
