#include "ModuleNote.hpp"


#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"

void Note::Serialize(JsonArchive& ar) {
    ar.Serialize("id", id);
    ar.Serialize("text", text);
    ar.Serialize("gpsTracker", gpsTracker);
    ar.Serialize("radioFrequency", radioFrequency);
}

void ModuleNote::OnGameMessage(const std::vector<std::string_view>& function,
                               const std::vector<std::string_view>& arguments) {}

void ModuleNote::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments,
    const std::function<void(std::string_view)>& replyFunc) {

    if (function.front() == "Create") {
        Note newNote;
        newNote.id = newNoteIdx++;
        notes.insert({ newNote.id, newNote });

        GNetworkController.SendStateUpdate();
    } else if (function.front() == "SetText") {
        int id = arguments["id"];

        if (!notes.contains(id)) return;

        notes[id].text = arguments["text"];

        GNetworkController.SendStateUpdate();
    } else if (function.front() == "SetGPS") {
        int id = arguments["id"];

        if (!notes.contains(id)) return;

        notes[id].gpsTracker = arguments["text"];

        GNetworkController.SendStateUpdate();
    } else if (function.front() == "SetRadio") {
        int id = arguments["id"];

        if (!notes.contains(id)) return;

        notes[id].radioFrequency = arguments["text"];

        GNetworkController.SendStateUpdate();
    }
}

void ModuleNote::SerializeState(JsonArchive& ar) {
    JsonArchive notesAr;
    //Want to pass empty object, instead of null
    *notesAr.getRaw() = nlohmann::json::object();
    for (auto& [key, value] : notes) {
        notesAr.Serialize(std::to_string(key).data(), value);
    }

    ar.Serialize("notes", notesAr);
}
