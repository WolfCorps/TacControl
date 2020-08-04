#include "RadioModule.hpp"
#include <ranges>


#include "Game/GameManager.hpp"
#include "Util/Util.hpp"
#include "Networking/Serialize.hpp"
#include "Util/Logger.hpp"

#include <fmt/format.h>

#include "Networking/NetworkController.hpp"

#undef SendMessage
//#TODO Find out how windows.h gets in here

void TFARRadio::Serialize(JsonArchive& ar) {
    ar.Serialize("id", id);
    ar.Serialize("displayName", displayName);
    ar.Serialize("currentChannel", currentChannel);
    ar.Serialize("currentAltChannel", currentAltChannel);
    ar.Serialize("rx", rx);
    ar.Serialize("tx", tx);
    ar.Serialize("channels", channels, [](JsonArchive& element, const RadioChannel& channel) {
        *element.getRaw() = channel.frequency;
    });
}

void RadioModule::DoNetworkUpdateRadio(TFARRadio& radio) {
    JsonArchive ar;
    radio.Serialize(ar);

   Logger::log(LoggerTypes::General, ar.to_string());
}

void RadioModule::OnRadioUpdate(const std::vector<std::string_view>& arguments) {
    auto radioId = arguments[0];

    auto found = FindOrCreateRadioByClassname(radioId);

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

    GNetworkController.SendStateUpdate();
}

void RadioModule::OnRadioTransmit(const std::vector<std::string_view>& arguments) {
    auto radioClass = arguments[0];
    auto radioChannel = arguments[1];
    auto radioStart = arguments[2];
    auto found = FindOrCreateRadioByClassname(radioClass);
    if (!Util::isTrue(radioStart)) {
        found->tx = -1;
    } else {
        found->tx = Util::parseArmaNumberToInt(radioChannel);
    }

    GNetworkController.SendStateUpdate();
}

std::vector<TFARRadio>::iterator RadioModule::FindOrCreateRadioByClassname(std::string_view classname) {
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

void RadioModule::OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {


    //#TODO https://github.com/michail-nikolaev/task-force-arma-3-radio/blob/master/ts/src/CommandProcessor.cpp#L121

    //#TODO dispatch to thread

    auto func = function[0];
    if (func == "RadioUpdate") {
        OnRadioUpdate(arguments);
    } else if (func == "Transmit") {
        OnRadioTransmit(arguments);
    } else if (func == "Test") {

        if (radios.empty()) return;
        auto& radio = radios.front();

        DoRadioTransmit(radio.id, radio.currentChannel, true);
    }
}

void RadioModule::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments) {

    if (function[0] == "Transmit") {
        std::string_view radioId = arguments["radioId"];
        int8_t channel = arguments["channel"];
        bool tx = arguments["tx"];

        DoRadioTransmit(radioId, channel, tx);
    }
}

void RadioModule::SerializeState(JsonArchive& ar) {

    auto fut = AddTask([this, &ar]() {
        ar.Serialize("radios", radios, [](JsonArchive& element, TFARRadio& radio) {
            radio.Serialize(element);
            });
        });
    fut.wait();
}



void RadioModule::DoRadioTransmit(std::string_view radioId, int8_t channel, bool transmitting) {
    GGameManager.SendMessage("Radio.Cmd.Transmit", fmt::format("{}\n{}\n{}", radioId, channel, transmitting));

}


