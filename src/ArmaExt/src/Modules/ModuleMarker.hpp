#pragma once
#include "Util/Module.hpp"
#include "Util/Thread.hpp"
#include "Util/Util.hpp"


class JsonArchive;


class ModuleMarker : public ThreadQueue, public IMessageReceiver, public IStateHolder, IPostInitReceiver, IPreInitReceiver {

    struct MarkerType {
        std::string name;
        std::string color;
        uint32_t size;
        bool shadow;
        std::string icon;

        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, MarkerType, std::less<>> markerTypes;

    struct MarkerColor {
        std::string name;
        std::string color;

        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, MarkerColor, std::less<>> markerColors;

    struct MarkerBrush {
        std::string name;
        std::string texture;
        bool drawBorder;

        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, MarkerBrush, std::less<>> markerBrushes;

    struct ActiveMarker {
        std::string id;
        std::string type;
        std::string color; //#TODO parse here already instead of frontend
        float dir;
        Vector3D pos;
        std::string text;
        std::string shape; //#TODO enum
        float alpha;
        std::string brush;
        std::string size; //xxx,yyy really just two numbers
        int channel;
        std::vector<Vector2D> polyline;

        // data for serialization

        // Marker was edited/added since last serialization
        bool hasChanged = false;


        void Serialize(JsonArchive& ar);
    };

    std::map<std::string, ActiveMarker, std::less<>> markers;
public:
    //ThreadQueue
    void ModulePostInit() override { SetThreadName("TacControl_Marker"); }

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Marker"sv; }
    void OnMarkerTypesRetrieved(const std::vector<std::basic_string_view<char>>& arguments);
    void OnMarkerDeleted(const std::vector<std::basic_string_view<char>>& arguments);
    void OnMarkerCreated(const std::vector<std::basic_string_view<char>>& arguments);
    void OnMarkerUpdated(const std::vector<std::basic_string_view<char>>& arguments);
    void OnGameMessage(const std::vector<std::string_view>& function,
                       const std::vector<std::string_view>& arguments) override;

    void OnDoCreateMarker(const nlohmann::json& arguments);
    void OnDoEditMarker(const nlohmann::json& arguments);
    void OnDoDeleteMarker(const nlohmann::json& arguments);

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() const override { return "Marker"sv; };
    void SerializeState(JsonArchive& ar) override;

    //IPostInitReceiver
    void OnGamePostInit() override;
    //IPreInitReceiver
    void OnGamePreInit() override;
};

inline ModuleMarker GModuleMarker;
