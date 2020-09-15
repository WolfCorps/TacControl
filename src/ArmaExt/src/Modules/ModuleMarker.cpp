#include "ModuleMarker.hpp"

#include "Game/GameManager.hpp"
#include "ModuleImageDirectory.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"
#include <fmt/format.h>
#undef SendMessage

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
    ar.Serialize("size", size);
    ar.Serialize("channel", channel);
    ar.Serialize("polyline", polyline);
}

void ModuleMarker::OnMarkerTypesRetrieved(const std::vector<std::basic_string_view<char>>& arguments) {
    auto types = Util::split(arguments[0], '\t');
    auto colors = Util::split(arguments[1], '\t');
    auto brushes = Util::split(arguments[2], '\t');
    playerDirectPlayID = arguments[3];

    for (auto& it : types) {
        auto split = Util::split(it, '\n');

        MarkerType type;

        auto classname = split[0];
        type.name = split[1];
        type.color = split[2];
        type.size = Util::parseArmaNumberToInt(split[3]);
        type.icon = split[4];
        type.shadow = Util::parseArmaNumberToInt(split[5]) > 0;

        AddTask([icon = type.icon]()
        {
            GModuleImageDirectory.LoadTextureToCache(icon); //#TODO do that in imageDirectory thread
        });

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
    std::string markerName = std::string(arguments[0]);

    AddTask([this, markerName = std::move(markerName)]()
    {
        auto found = markers.find(markerName);
        if (found != markers.end()) markers.erase(found);

        GNetworkController.SendStateUpdate("Marker");
    });
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
    newMarker.size = arguments[9];
    newMarker.channel = Util::parseArmaNumberToInt(arguments[10]);
    auto polyLineStr = arguments[11];
    newMarker.polyline.clear();
    auto polySplit = Util::split(polyLineStr, ';');
    if (polySplit.size() % 2 == 0)
        for (auto i = 0u; i < polySplit.size(); i+=2) {
            newMarker.polyline.push_back(Vector2D(polySplit[i], polySplit[i + 1]));
        }

    AddTask([this, newMarker = std::move(newMarker)]()
    {
        markers[newMarker.id] = newMarker;
        GNetworkController.SendStateUpdate("Marker");
    });
}

void ModuleMarker::OnMarkerUpdated(const std::vector<std::basic_string_view<char>>& arguments) {
    //["Marker.Update", _marker call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;

    ActiveMarker newMarker; //#TODO use one func that parses markerInfo, don't duplicate code with OnMarkerCreated
    newMarker.id = arguments[0];
    newMarker.type = arguments[1];
    newMarker.color = arguments[2];
    newMarker.dir = Util::parseArmaNumber(arguments[3]);
    newMarker.pos = Vector3D(arguments[4]);
    newMarker.text = arguments[5];
    newMarker.shape = arguments[6];
    newMarker.alpha = Util::parseArmaNumber(arguments[7]);
    newMarker.brush = arguments[8];
    newMarker.size = arguments[9];
    newMarker.channel = Util::parseArmaNumberToInt(arguments[10]);
    auto polyLineStr = arguments[11];
    newMarker.polyline.clear();
    auto polySplit = Util::split(polyLineStr, ';');
    if (polySplit.size() % 2 == 0)
        for (auto i = 0u; i < polySplit.size(); i += 2) {
            newMarker.polyline.push_back(Vector2D(polySplit[i], polySplit[i + 1]));
        }

    AddTask([this, newMarker = std::move(newMarker)]()
    {
        markers[newMarker.id] = newMarker;
        GNetworkController.SendStateUpdate("Marker");
    });
}

void ModuleMarker::OnGameMessage(const std::vector<std::string_view>& function,
                                 const std::vector<std::string_view>& arguments) {
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

void ModuleMarker::OnDoCreateMarker(const nlohmann::json& arguments) {


    std::string markerName = arguments["name"];
    std::string markerType = arguments["type"];
    std::string markerColor = arguments["color"];
    float markerDir = arguments["dir"];

    const nlohmann::json& posRef = arguments["pos"];
    JsonArchive PosAr(posRef);
    Vector3D markerPos;
    markerPos.Serialize(PosAr);
    std::string markerText = arguments["text"];
    std::string markerShape = arguments["shape"];
    float markerAlpha = arguments["alpha"];
    std::string markerBrush = arguments["brush"];
    std::string markerSize = arguments["size"]; // "[1,2]"
    int markerChannel = arguments["channel"];
    std::vector<float> polyLine;
    for (auto& it : arguments["polyline"]) {
        polyLine.emplace_back((float)it[0]);
        polyLine.emplace_back((float)it[1]);
    }

    std::string polyLineString = "[";
    for (auto& it : polyLine) {
        polyLineString += std::to_string(it);
        polyLineString += ",";
    }

    if (!polyLine.empty()) //remove last comma
        polyLineString.pop_back();
    polyLineString += "]";


    auto args = fmt::format("{}\n{}\n{}\n{}\n{}\n{}\n{}\n{}\n{}\n{}\n{}\n{}",
        markerName,
        markerType,
        markerColor,
        markerDir,
        markerPos.toString(),
        markerText.empty() ? " " : markerText, //cannot be empty otherwise splitString parsing fails, whitespace works
        markerShape,
        markerAlpha,
        markerBrush,
        markerSize,
        markerChannel,
        polyLineString
    );
    GGameManager.SendMessage("Marker.Cmd.CreateMarker", args);
}

void ModuleMarker::OnDoDeleteMarker(const nlohmann::json& arguments) {


    GGameManager.SendMessage("Marker.Cmd.DeleteMarker", arguments["name"]);
}

void ModuleMarker::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {
    if (function[0] == "CreateMarker") {
        OnDoCreateMarker(arguments);
    }
    if (function[0] == "DeleteMarker") {
        OnDoDeleteMarker(arguments);
    }
}

void ModuleMarker::SerializeState(JsonArchive& ar) {

    auto fut = AddTask([this, &ar]() {

        if (!ar.HasKey("markerTypes")) { //if we just update existing state, don't repush stuff that's already present
            JsonArchive markerTypesAr;
            //Want to pass empty object, instead of null
            *markerTypesAr.getRaw() = nlohmann::json::object();
            for (auto& [key, value] : markerTypes) {
                markerTypesAr.Serialize(key.data(), value);
            }
            ar.Serialize("markerTypes", markerTypesAr);
        }

        if (!ar.HasKey("markerColors")) {
            JsonArchive markerColorsAr;
            //Want to pass empty object, instead of null
            *markerColorsAr.getRaw() = nlohmann::json::object();
            for (auto& [key, value] : markerColors) {
                markerColorsAr.Serialize(key.data(), value);
            }

            ar.Serialize("markerColors", markerColorsAr);
        }

        if (!ar.HasKey("markerBrushes")) {
            JsonArchive markerBrushesAr;
            //Want to pass empty object, instead of null
            *markerBrushesAr.getRaw() = nlohmann::json::object();
            for (auto& [key, value] : markerBrushes) {
                markerBrushesAr.Serialize(key.data(), value);
            }

            ar.Serialize("markerBrushes", markerBrushesAr);
        }

        JsonArchive markersAr;
        //Want to pass empty object, instead of null
        *markersAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : markers) {
            markersAr.Serialize(key.data(), value);
        }

        ar.Serialize("playerDirectPlayID", playerDirectPlayID);

        ar.Serialize("markers", markersAr);

        });
    fut.wait();



}

void ModuleMarker::OnGamePostInit() {
    if (markerTypes.empty())
        GGameManager.SendMessage("Marker.Cmd.GetMarkerTypes", "");
}

void ModuleMarker::OnGamePreInit() {
    markers.clear();
}
