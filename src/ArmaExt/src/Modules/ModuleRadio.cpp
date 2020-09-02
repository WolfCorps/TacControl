#include "ModuleRadio.hpp"
#include <ranges>


#include "Game/GameManager.hpp"
#include "Util/Util.hpp"
#include "Networking/Serialize.hpp"
#include "Util/Logger.hpp"

#include "Networking/NetworkController.hpp"

void TFARRadio::Serialize(JsonArchive& ar) {
    ar.Serialize("id", id);
    ar.Serialize("displayName", displayName);
    ar.Serialize("currentChannel", currentChannel);
    ar.Serialize("currentAltChannel", currentAltChannel);
    ar.Serialize("mainStereo", mainStereo);
    ar.Serialize("altStereo", altStereo);
    ar.Serialize("volume", volume);
    ar.Serialize("speaker", speaker);
    ar.Serialize("rx", rx);
    ar.Serialize("tx", tx);
    ar.Serialize("channels", channels, [](JsonArchive& element, const RadioChannel& channel) {
        *element.getRaw() = channel.frequency;
    });
}

void ModuleRadio::DoNetworkUpdateRadio(TFARRadio& radio) {
    JsonArchive ar;
    radio.Serialize(ar);

   Logger::log(LoggerTypes::General, ar.to_string());
}

void ModuleRadio::OnRadioUpdate(const std::vector<std::string_view>& arguments) {
    auto radioId = arguments[0];

    auto found = FindOrCreateRadioById(radioId);

    if (found->displayName.empty())
        found->displayName = radioId;

    found->currentChannel = Util::parseArmaNumberToInt(arguments[1]);
    found->currentAltChannel = Util::parseArmaNumberToInt(arguments[2]);


    found->channels.clear();
    auto radioChannels = Util::split(arguments[3], '\n');
    std::transform(radioChannels.begin(), radioChannels.end(), std::back_inserter(found->channels), [](std::string_view freq)
        {
            TFARRadio::RadioChannel res;
            res.frequency = freq;
            return res;
        });

    found->mainStereo = Util::parseArmaNumberToInt(arguments[4]);
    found->altStereo = Util::parseArmaNumberToInt(arguments[5]);
    found->volume = Util::parseArmaNumberToInt(arguments[6]);
    found->speaker = Util::isTrue(arguments[7]);



    GNetworkController.SendStateUpdate("Radio");
}

void ModuleRadio::OnRadioTransmit(const std::vector<std::string_view>& arguments) {
    auto radioClass = arguments[0];
    auto radioChannel = arguments[1];
    auto radioStart = arguments[2];
    auto found = FindOrCreateRadioById(radioClass);
    if (!Util::isTrue(radioStart)) {
        found->tx = -1;
    } else {
        found->tx = Util::parseArmaNumberToInt(radioChannel);
    }

    GNetworkController.SendStateUpdate("Radio");
}

void ModuleRadio::OnRadioDelete(const std::vector<std::string_view>& arguments) {
    auto radioClass = arguments[0];

    auto found = FindOrCreateRadioById(radioClass);
    radios.erase(found);
 
    GNetworkController.SendStateUpdate();
}

std::vector<TFARRadio>::iterator ModuleRadio::FindOrCreateRadioById(std::string_view classname) {
    auto found = std::ranges::find_if(radios, [classname](const TFARRadio& rad) {
        return rad.id == classname;
        });

    if (found != radios.end()) {
        return found;
    } else {
        radios.emplace_back(TFARRadio());
        radios.back().id = classname;
        return radios.end() - 1;
    }
}

std::optional<std::reference_wrapper<TFARRadio>> ModuleRadio::FindRadioById(std::string_view classname) {
    auto found = std::ranges::find_if(radios, [classname](const TFARRadio& rad) {
        return rad.id == classname;
        });

    if (found != radios.end()) {
        return *found;
    }
    else {
        return std::nullopt;
    }
}

void ModuleRadio::OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {


    //#TODO https://github.com/michail-nikolaev/task-force-arma-3-radio/blob/master/ts/src/CommandProcessor.cpp#L121

    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "RadioUpdate") {
        OnRadioUpdate(arguments);
    } else if (func == "Transmit") {
        OnRadioTransmit(arguments);
    } else if (func == "RadioDelete") {
        OnRadioDelete(arguments);
    } else if (func == "Test") {

        if (radios.empty()) return;
        auto& radio = radios.front();

        DoRadioTransmit(radio.id, radio.currentChannel, true);
    }
}

void ModuleRadio::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {

    if (function[0] == "Transmit") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        int8_t channel = arguments["channel"];
        bool tx = arguments["tx"];

        DoRadioTransmit(radioId, channel, tx);
    } else if (function[0] == "SetStereo") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        bool isMain = arguments["main"];
        int mode = arguments["mode"];

        GGameManager.SendMessage("Radio.Cmd.SetStereo", fmt::format("{}\n{}\n{}", radioId, isMain, mode));
    } else if (function[0] == "SetSpeaker") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        bool enabled = arguments["enabled"];

        GGameManager.SendMessage("Radio.Cmd.SetSpeaker", fmt::format("{}\n{}", radioId, enabled));
    } else if (function[0] == "SetFrequency") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        int8_t channel = arguments["channel"];
        std::string freq = arguments["freq"];

        GGameManager.SendMessage("Radio.Cmd.SetFrequency", fmt::format("{}\n{}\n{}", radioId, channel, freq));
    } else if (function[0] == "SetVolume") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        int volume = arguments["volume"];

        GGameManager.SendMessage("Radio.Cmd.SetVolume", fmt::format("{}\n{}", radioId, volume));
    } else if (function[0] == "SetChannel") {
        std::string_view radioId = arguments["radioId"];
        if (!FindRadioById(radioId)) return;
        bool isMain = arguments["main"];
        int8_t channel = arguments["channel"];

        GGameManager.SendMessage("Radio.Cmd.SetChannel", fmt::format("{}\n{}\n{}", radioId, isMain, channel));
    } else if (function[0] == "SetDisplayName") {
        std::string_view radioId = arguments["radioId"];
        auto radioRef = FindRadioById(radioId);
        if (!radioRef) return;
        radioRef->get().displayName = arguments["name"];
        GNetworkController.SendStateUpdate("Radio");
    }
}

void ModuleRadio::SerializeState(JsonArchive& ar) {

    auto fut = AddTask([this, &ar]() {
        ar.Serialize("radios", radios, [](JsonArchive& element, TFARRadio& radio) {
            radio.Serialize(element);
            });
        });
    fut.wait();
}

void ModuleRadio::OnGamePreInit() {
    radios.clear();
}


void ModuleRadio::DoRadioTransmit(std::string_view radioId, int8_t channel, bool transmitting) {
    GGameManager.SendMessage("Radio.Cmd.Transmit", fmt::format("{}\n{}\n{}", radioId, channel, transmitting));
}

std::optional<TFARRadio> ModuleRadio::GetFirstSRRadio() {
    auto firstRadio = std::ranges::find_if(radios, [](const TFARRadio& rad)
        {
            return rad.id.find(' ') == std::string::npos;
        });
    if (firstRadio == radios.end()) return {};
    return *firstRadio;
}

std::optional<TFARRadio> ModuleRadio::GetFirstLRRadio() {
    auto firstRadio = std::ranges::find_if(radios, [](const TFARRadio& rad)
        {
            return rad.id.find(' ') != std::string::npos;
        });
    if (firstRadio == radios.end()) return {};
    return *firstRadio;
}


