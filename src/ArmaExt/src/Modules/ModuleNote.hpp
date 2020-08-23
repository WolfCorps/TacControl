#pragma once
#include <optional>

#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;

struct Note {
    int id;
    std::string text;
    std::string gpsTracker;
    std::string radioFrequency;

    void Serialize(JsonArchive& ar);
};


class ModuleNote : public ThreadQueue, public IMessageReceiver, public IStateHolder {

    std::map<int, Note> notes;
    int newNoteIdx = 0;
public:
    //ThreadQueue
    void ModulePostInit() override { SetThreadName("TacControl_ModuleNote"); }

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Note"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "Note"sv; };
    void SerializeState(JsonArchive& ar) override;
};

inline ModuleNote GModuleNote;
