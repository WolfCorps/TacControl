#pragma once
#include "Util/Module.hpp"
#include "Util/Thread.hpp"
#include "Util/Util.hpp"


class JsonArchive;

struct GPSTracker {
    std::string id;
    std::string displayName;
    Vector3D position;
    Vector3D velocity;
    bool _update = false; //Internal use to track if tracker was included in last update

    void Serialize(JsonArchive& ar);
};


class ModuleGPS : public ThreadQueuePeriodic, public IMessageReceiver, public IStateHolder {
    std::map<std::string, GPSTracker, std::less<>> trackers;

    void DoNetworkUpdate();
    GPSTracker& FindOrCreateTrackerByID(const std::string_view& cs_);
    void OnTrackerUpdate(const std::vector<std::string_view>& arguments);
public:





    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "GPS"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "GPS"sv; };
    void SerializeState(JsonArchive& ar) override;
};

inline ModuleGPS GModuleGPS;
