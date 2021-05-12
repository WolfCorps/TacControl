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
    uint8_t mainStereo = 0; // 0 center, 1 left, 2 right
    uint8_t altStereo = 0; // 0 center, 1 left, 2 right
    uint8_t volume = 70; //0-10
    bool speaker = false;
    //#TODO can receive on multiple channels..
    bool rx = false; //receiving right now
    //-1 == not sending
    int8_t tx = -1; //sending right now, number is channel number 0 based index
    std::vector<RadioChannel> channels;

    void Serialize(JsonArchive& ar);

    std::optional<RadioChannel> GetChannel(int8_t index) {
        if (index < 0 || index >= channels.size()) return {};
        return channels[index];
    }

    std::optional<RadioChannel> GetCurrentChannel() {
        return GetChannel(currentChannel);
    }

    std::optional<RadioChannel> GetCurrentAltChannel() {
        return GetChannel(currentAltChannel);
    }

};


class ModuleRadio : public ThreadQueue, public IMessageReceiver, public IStateHolder, IPreInitReceiver {

    std::vector<TFARRadio> radios;

    void DoNetworkUpdateRadio(TFARRadio& radio);
    void OnRadioUpdate(const std::vector<std::string_view>& arguments);
    void OnRadioTransmit(const std::vector<std::string_view>& arguments);
    void OnRadioDelete(const std::vector<std::string_view>& arguments);

    std::vector<TFARRadio>::iterator FindOrCreateRadioById(std::string_view classname);
    std::optional<std::reference_wrapper<TFARRadio>> FindRadioById(std::string_view classname);
public:
    //ThreadQueue
    void ModulePostInit() override { SetThreadName("TacControl_Radio"); }

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Radio"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
                       const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(ReplyMessageType)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() const override { return "Radio"sv; }
    void SerializeState(JsonArchive& ar) override;

    //IPreInitReceiver
    void OnGamePreInit() override;

    void DoRadioTransmit(std::string_view radioId, int8_t channel, bool transmitting);


    std::optional<TFARRadio> GetFirstSRRadio();
    std::optional<TFARRadio> GetFirstLRRadio();


};

inline ModuleRadio GModuleRadio;
