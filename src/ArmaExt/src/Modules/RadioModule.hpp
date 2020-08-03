#pragma once
#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;

struct RadioFrequency {
    std::string radioClass;
    std::string currentFreq;
    int currentChannel;
    bool isTransmitting;


    RadioFrequency(std::string_view radClass, std::string_view freq): radioClass(radClass), currentFreq(freq) {}


    void Serialize(JsonArchive& ar);
};


class RadioModule : public ThreadQueue, public IMessageReceiver {

    std::vector<RadioFrequency> radios;


    void DoNetworkUpdateRadio(RadioFrequency& radio);
    void OnRadioUpdate(const std::vector<std::string_view>& arguments);
    void OnRadioTransmit(const std::vector<std::string_view>& arguments);

    std::vector<RadioFrequency>::iterator FindOrCreateRadioByClassname(std::string_view classname);



public:






    std::string_view GetMessageReceiverName() override { return "Radio"; }
    void OnGameMessage(const std::vector<std::string_view>& function,
                       const std::vector<std::string_view>& arguments) override;
};

static inline RadioModule GRadioModule;