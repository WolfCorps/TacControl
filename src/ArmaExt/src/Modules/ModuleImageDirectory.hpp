#pragma once
#include <filesystem>
#include <utility>

#include "Util/Module.hpp"
#include "Util/Thread.hpp"


class JsonArchive;


class ModuleImageDirectory : public ThreadQueue, public IMessageReceiver {

    struct PrefixPboFile {
        std::filesystem::path file;
        std::string prefix;

        PrefixPboFile(std::filesystem::path a, std::string b) : file(std::move(a)), prefix(std::move(b)) {}
    };

    std::vector<PrefixPboFile> pboWithPrefixes;
    std::atomic_bool pboPrefixesReady;

    void LoadPboPrefixes();

    decltype(pboWithPrefixes)::iterator FindPboByFilepath(std::string_view path);

    std::vector<char> LoadFileToBuffer(std::string_view path);

    //RGBA 8-8-8-8, width, heigth
    std::tuple<std::vector<char>, int, int> LoadRGBATexture(std::string_view path);

    std::mutex cacheLock;
    std::map<std::string, std::tuple<std::vector<char>, int, int>, std::less<>> imageCache;

    nlohmann::json generateMapfileMessage(std::string_view path);

    std::vector<std::function<void(std::string_view)>> waitingForMapExport;



public:
    void LoadTextureToCache(std::string_view path);

    void ModulePostInit() override;

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "ImgDir"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;
    bool IsReceiveGameMessageAsync() const override { return false; }

    
};

inline ModuleImageDirectory GModuleImageDirectory;
