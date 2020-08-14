#pragma once
#include <optional>

#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;

struct TFARRadio {

    struct RadioChannel {
        std::string frequency;
    };


    std::string id;
    std::string displayName;
    int8_t currentChannel = -1;
    int8_t currentAltChannel = -1;
    //#TODO can receive on multiple channels..
    bool rx = false; //receiving right now
    //-1 == not sending
    int8_t tx = -1; //sending right now, number is channel number 0 based index
    std::vector<RadioChannel> channels;

    void Serialize(JsonArchive& ar);
};


class RadioModule : public ThreadQueue, public IMessageReceiver, public IStateHolder, IPreInitReceiver {

    std::vector<TFARRadio> radios;

    void DoNetworkUpdateRadio(TFARRadio& radio);
    void OnRadioUpdate(const std::vector<std::string_view>& arguments);
    void OnRadioTransmit(const std::vector<std::string_view>& arguments);
    void OnRadioDelete(const std::vector<std::string_view>& arguments);

    std::vector<TFARRadio>::iterator FindOrCreateRadioByClassname(std::string_view classname);
public:

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Radio"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
                       const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "Radio"sv; };
    void SerializeState(JsonArchive& ar) override;

    //IPreInitReceiver
    void OnGamePreInit() override;

    void DoRadioTransmit(std::string_view radioId, int8_t channel, bool transmitting);



};

inline RadioModule GRadioModule;
