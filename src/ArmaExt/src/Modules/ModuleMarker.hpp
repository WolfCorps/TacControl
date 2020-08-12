#pragma once
#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;


class ModuleMarker : public ThreadQueue, public IMessageReceiver, public IStateHolder, IPostInitReceiver {

    struct MarkerType {
        std::string name;
        std::string color;
        uint32_t size;
        bool shadow;
        std::string icon;
    };

    std::map<std::string, MarkerType, std::less<>> markerTypes;

    struct MarkerColor {
        std::string name;
        std::string color;
    };

    std::map<std::string, MarkerColor, std::less<>> markerColors;

    struct MarkerBrush {
        std::string name;
        std::string texture;
        bool drawBorder;
    };

    std::map<std::string, MarkerBrush, std::less<>> markerBrushes;

public:





    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Marker"sv; }
    void OnMarkerTypesRetrieved(const std::vector<std::basic_string_view<char>>& cses_);
    void OnGameMessage(const std::vector<std::string_view>& function,
                       const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "Marker"sv; };
    void SerializeState(JsonArchive& ar) override;

    //IPostInitReceiver
    void OnGamePostInit() override;
};

inline ModuleMarker GModuleMarker;
