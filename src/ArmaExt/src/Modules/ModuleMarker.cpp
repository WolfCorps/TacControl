#include "ModuleMarker.hpp"

#include "Game/GameManager.hpp"
#include "ModuleImageDirectory.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"

void ModuleMarker::MarkerType::Serialize(JsonArchive& ar) {
    ar.Serialize("name", name);
    ar.Serialize("color", color);
    ar.Serialize("size", size);
    ar.Serialize("shadow", shadow);
    ar.Serialize("icon", icon);
}

void ModuleMarker::MarkerColor::Serialize(JsonArchive& ar) {
    ar.Serialize("name", name);
    ar.Serialize("color", color);
}

void ModuleMarker::MarkerBrush::Serialize(JsonArchive& ar) {
    ar.Serialize("name", name);
    ar.Serialize("texture", texture);
    ar.Serialize("drawBorder", drawBorder);
}

void ModuleMarker::ActiveMarker::Serialize(JsonArchive& ar) {

    ar.Serialize("id", id);
    ar.Serialize("type", type);
    ar.Serialize("color", color);
    ar.Serialize("dir", dir);
    ar.Serialize("pos", pos);
    ar.Serialize("text", text);
    ar.Serialize("shape", shape);
    ar.Serialize("alpha", alpha);
    ar.Serialize("brush", brush);
}

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

void ModuleMarker::OnMarkerDeleted(const std::vector<std::basic_string_view<char>>& arguments) {
    auto markerName = arguments[0];

    auto found = markers.find(markerName);
    if (found != markers.end()) markers.erase(found);

    GNetworkController.SendStateUpdate();
}

void ModuleMarker::OnMarkerCreated(const std::vector<std::basic_string_view<char>>& arguments) {
    //["Marker.Create", _marker call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;

    ActiveMarker newMarker;
    newMarker.id = arguments[0];
    newMarker.type = arguments[1];
    newMarker.color = arguments[2];
    newMarker.dir = Util::parseArmaNumber(arguments[3]);
    newMarker.pos = Vector3D(arguments[4]);
    newMarker.text = arguments[5];
    newMarker.shape = arguments[6];
    newMarker.alpha = Util::parseArmaNumber(arguments[7]);
    newMarker.brush = arguments[8];

    markers[newMarker.id] = newMarker;
    GNetworkController.SendStateUpdate();
}

void ModuleMarker::OnMarkerUpdated(const std::vector<std::basic_string_view<char>>& arguments) {
    //["Marker.Update", _marker call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;

    ActiveMarker newMarker;
    newMarker.id = arguments[0];
    newMarker.type = arguments[1];
    newMarker.color = arguments[2];
    newMarker.dir = Util::parseArmaNumber(arguments[3]);
    newMarker.pos = Vector3D(arguments[4]);
    newMarker.text = arguments[5];
    newMarker.shape = arguments[6];
    newMarker.alpha = Util::parseArmaNumber(arguments[7]);
    newMarker.brush = arguments[8];

    markers[newMarker.id] = newMarker;
    GNetworkController.SendStateUpdate();
}

void ModuleMarker::OnGameMessage(const std::vector<std::string_view>& function,
                                 const std::vector<std::string_view>& arguments) {

    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "MarkerTypes") {
        OnMarkerTypesRetrieved(arguments);
    } else if (func == "Create") {
        OnMarkerCreated(arguments);
    } else if (func == "Update") {
        OnMarkerUpdated(arguments);
    } else if (func == "Delete") {
        OnMarkerDeleted(arguments);
    }








}

void ModuleMarker::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {
    
}

void ModuleMarker::SerializeState(JsonArchive& ar) {

    auto fut = AddTask([this, &ar]() {


        JsonArchive markerTypesAr;
        //Want to pass empty object, instead of null
        *markerTypesAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : markerTypes) {
            markerTypesAr.Serialize(key.data(), value);
        }

        ar.Serialize("markerTypes", markerTypesAr);

        JsonArchive markerColorsAr;
        //Want to pass empty object, instead of null
        *markerColorsAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : markerColors) {
            markerColorsAr.Serialize(key.data(), value);
        }

        ar.Serialize("markerColors", markerColorsAr);

        JsonArchive markerBrushesAr;
        //Want to pass empty object, instead of null
        *markerBrushesAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : markerBrushes) {
            markerBrushesAr.Serialize(key.data(), value);
        }

        ar.Serialize("markerBrushes", markerBrushesAr);


        JsonArchive markersAr;
        //Want to pass empty object, instead of null
        *markersAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : markers) {
            markersAr.Serialize(key.data(), value);
        }

        ar.Serialize("markers", markersAr);



        });
    fut.wait();



}

void ModuleMarker::OnGamePostInit() {
    GGameManager.SendMessage("Marker.Cmd.GetMarkerTypes", "");
}
