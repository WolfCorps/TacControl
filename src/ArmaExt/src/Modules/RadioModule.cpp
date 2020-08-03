#include "RadioModule.hpp"
#include <ranges>

#include "Util/Util.hpp"
#include "Networking/Serialize.hpp"
#include "Util/Logger.hpp"

void RadioFrequency::Serialize(JsonArchive& ar) {
    ar.Serialize("radioClass", radioClass);
    ar.Serialize("currentFreq", currentFreq);
    ar.Serialize("currentChannel", currentChannel);
    ar.Serialize("isTransmitting", isTransmitting);
}

void RadioModule::DoNetworkUpdateRadio(RadioFrequency& radio) {
    JsonArchive ar;
    radio.Serialize(ar);

   Logger::log(LoggerTypes::General, ar.to_string());
}

void RadioModule::OnRadioUpdate(const std::vector<std::string_view>& arguments) {
    auto radioClass = arguments[0];
    auto radioFreq = arguments[1];

    auto found = FindOrCreateRadioByClassname(radioClass);
    found->currentFreq = radioFreq;

    //#Network
    DoNetworkUpdateRadio(*found);
}

void RadioModule::OnRadioTransmit(const std::vector<std::string_view>& arguments) {
    auto radioClass = arguments[0];
    auto radioChannel = arguments[1];
    auto radioStart = arguments[2];
    auto found = FindOrCreateRadioByClassname(radioClass);
    found->currentChannel = Util::parseArmaNumberToInt(radioChannel);
    found->isTransmitting = Util::isTrue(radioStart);

    DoNetworkUpdateRadio(*found);
}

std::vector<RadioFrequency>::iterator RadioModule::FindOrCreateRadioByClassname(std::string_view classname) {
    auto found = std::ranges::find_if(radios, [classname](const RadioFrequency& rad) {
        return rad.radioClass == classname;
        });

    if (found != radios.end()) {
        return found;
    } else {
        radios.emplace_back(RadioFrequency{ classname, "" });
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


    }




}
