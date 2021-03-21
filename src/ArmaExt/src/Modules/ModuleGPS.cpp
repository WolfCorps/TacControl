#include "ModuleGPS.hpp"



#include "Game/GameManager.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"


void GPSTracker::Serialize(JsonArchive& ar) {
    ar.Serialize("id", id);
    ar.Serialize("pos", position);
    ar.Serialize("vel", velocity);
    ar.Serialize("displayName", displayName);
}

GPSTracker& ModuleGPS::FindOrCreateTrackerByID(const std::string_view& cs_) {
    //auto found = trackers.find(cs_);
    //if (found == trackers.end()) {
    //    
    //}

    auto res = trackers.emplace(cs_, GPSTracker());
    return res.first->second;
}


void ModuleGPS::OnTrackerUpdate(const std::vector<std::string_view>& arguments) {
    //[0] = "[\"\",[4255.51,4190.86,5],[0,0,0]]"
    bool wasEmpty = trackers.empty();
    for (auto& tracker : trackers) {
        tracker.second._update = false;
    }


    for (auto& it : arguments) {
        auto trimmed = Util::trim(it, "[]");
        auto split = Util::split(trimmed, '\n');
        // netid quoted, position, velocity
        auto netID = Util::trim(split[0], "\"");
        auto position = Vector3D(split[1]);
        auto velocity = Vector3D(split[2]);

        GPSTracker& tracker = FindOrCreateTrackerByID(netID);
        tracker.id = netID;
        tracker.position = position;
        tracker.velocity = velocity;
        tracker._update = true;
        //Only set this on new tracker
        if (tracker.displayName.empty()) tracker.displayName = netID;
    }

    std::erase_if(trackers, [](const std::pair<const std::string, GPSTracker>& tracker) {
        return !tracker.second._update;
    });

    GNetworkController.SendStateUpdate();

    if (trackers.empty())
        RemovePeriodicTask("trackerUpdate");
    else if (wasEmpty) {
        AddPeriodicTask("trackerUpdate", 1s, []() {
                GGameManager.SendMessage("GPS.Cmd.UpdateTrackers", "");
            });
    }
}

void ModuleGPS::OnGameMessage(const std::vector<std::string_view>& function,
                              const std::vector<std::string_view>& arguments) {
    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "TrackerUpdate") {
        OnTrackerUpdate(arguments);
    }
}

void ModuleGPS::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {

    if (function[0] == "SetTrackerName") {
        std::string_view trackerId = arguments["tracker"];
        std::string newName = arguments["name"];

        GPSTracker& tracker = FindOrCreateTrackerByID(trackerId);
        tracker.displayName = newName;

        GNetworkController.SendStateUpdate();
    }
}

void ModuleGPS::SerializeState(JsonArchive& ar) {
    auto fut = AddTask([this, &ar]() {
        JsonArchive trackersAr;
        //Want to pass empty object, instead of null
        *trackersAr.getRaw() = nlohmann::json::object();
        for (auto& [key, value] : trackers) {
            trackersAr.Serialize(key.data(), value);
        }

        ar.Serialize("trackers", std::move(trackersAr));
    });
    fut.wait();
}
