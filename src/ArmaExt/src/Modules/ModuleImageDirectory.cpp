#include "ModuleImageDirectory.hpp"
#include <ranges>
#include <algorithm>
#include <pbo.hpp>
#include <TextureFile.hpp>
#include "Util/Util.hpp"
#include <base64.h>
#include <s3tc.h>
#include <minilzo.h>


#include "ModuleGameInfo.hpp"
#include "Game/GameManager.hpp"
#include "Networking/NetworkController.hpp"

void ModuleImageDirectory::LoadPboPrefixes() {

    auto pboList = Util::GenerateLoadedPBOList();

    for (auto& it : pboList) {
        std::ifstream input(it, std::ifstream::binary);
        PboReader preader(input);
        preader.readHeadersSparse();
        auto& properties = preader.getProperties();
        auto found = std::ranges::find_if(properties, [](const PboProperty& prop)
        {
                return prop.key == "prefix"sv;
        });
        if (found == properties.end()) continue;

        auto prefix = found->value;


        pboWithPrefixes.emplace_back(
            it,
            Util::toLower(Util::trim(prefix, "\\"))
        );

    }

    std::sort(pboWithPrefixes.begin(), pboWithPrefixes.end(), [](const PrefixPboFile& l, const PrefixPboFile& r)
    {
            return l.prefix > r.prefix;
    });
    pboPrefixesReady = true;
}

void ModuleImageDirectory::ModulePostInit() {
    SetThreadName("TacControl_ImgDir");
    AddTask([this] { LoadPboPrefixes(); });
}

decltype(ModuleImageDirectory::pboWithPrefixes)::iterator ModuleImageDirectory::FindPboByFilepath(std::string_view path) {
    path = Util::trim(path, "\\");

    if (!pboPrefixesReady) pboPrefixesReady.wait(true);

    auto found = std::ranges::find_if(pboWithPrefixes, [path](const PrefixPboFile& pbo)
        {
            return path.starts_with(pbo.prefix);
        });

    return found;
}

std::vector<char> ModuleImageDirectory::LoadFileToBuffer(std::string_view path) {
    auto lowered = Util::toLower(path);
    path = Util::trim(lowered, "\\");

    auto pboFile = FindPboByFilepath(path);
    if (pboFile == pboWithPrefixes.end()) return {}; //Prefix not found
    //#TODO cache found pbo

    std::ifstream input(pboFile->file, std::ifstream::binary);
    PboReader preader(input);
    preader.readHeaders();

    auto& files = preader.getFiles();

    path.remove_prefix(pboFile->prefix.length());
    path = Util::trim(path, "\\");

    auto foundFile = std::ranges::find_if(files, [path](const PboEntry& file)
        {
            return Util::stringEqualsCaseInsensitive(file.name,path);
        });
    if (foundFile == files.end()) return {}; //File not found
  
    auto fs = preader.getFileBuffer(*foundFile);
    std::istream source(&fs);

    std::vector<char> result;
    result.resize(foundFile->original_size);

    source.read(result.data(), foundFile->original_size);
    return result;
}

template<typename CharT, typename TraitsT = std::char_traits<CharT> >
class vectorwrapbuf : public std::basic_streambuf<CharT, TraitsT> {
    using base = std::basic_streambuf<CharT, TraitsT>;
public:
    vectorwrapbuf(std::vector<CharT>& vec) {
        base::setg(vec.data(), vec.data(), vec.data() + vec.size());
    }

    typename base::pos_type seekoff(
        typename base::off_type off, std::ios_base::seekdir dir, std::ios_base::openmode) override {

        auto first = base::eback();

        if (dir == 0)
            base::setg(first, first + off, base::egptr());
        else if (dir == 1)
            base::setg(first, base::gptr() + off, base::egptr());

        return std::streampos(std::distance(first, base::gptr()));
    }


};

std::tuple<std::vector<char>, int, int> ModuleImageDirectory::LoadRGBATexture(std::string_view path) {
    std::unique_lock lock(cacheLock);

    auto found = imageCache.find(path);
    if (found != imageCache.end()) {
        return found->second;
    }
    lock.unlock();

    auto paaData = LoadFileToBuffer(path);
    if (paaData.empty()) return {};

    TextureFile texFile;
    texFile.doLogging = false;

    vectorwrapbuf<char> databuf(paaData);
    std::istream is(&databuf);
    texFile.readFromStream(is);

    if (texFile.mipmaps.empty())
        return {}; // ??? Happened once, probably corrupted mod. paaData was 26k of null bytes

    //Find biggest UNCOMPRESSED Mip
    auto biggestMip = *std::max_element(texFile.mipmaps.begin(), texFile.mipmaps.end(), [](const std::shared_ptr<MipMap>& l, const std::shared_ptr<MipMap>& r)
        {
            auto lSize = l->getRealSize();
            auto rSize = r->getRealSize();
            //if (l->isCompressed()) lSize = 0;
            //if (r->isCompressed()) rSize = 0;
            return lSize < rSize;
        });

    std::vector<char> output;
    uint32_t width = biggestMip->getRealSize();
    uint32_t height = biggestMip->height;
    output.resize(width * height * 4);

    std::vector<char>* DXTData = &biggestMip->data;
    std::vector<char> decompress;

    if (biggestMip->isCompressed()) {
        uint32_t expectedResultSize = width * height;
        if (texFile.type == PAAType::DXT1)
            expectedResultSize /= 2;

        lzo_uint dataSize = expectedResultSize;

        decompress.resize(expectedResultSize);
        lzo1x_decompress(reinterpret_cast<unsigned char*>(biggestMip->data.data()), biggestMip->data.size(), reinterpret_cast<unsigned char*>(decompress.data()), &dataSize, nullptr);
        if (dataSize != expectedResultSize)
            decompress.resize(dataSize);
        DXTData = &decompress;
    }


    if (texFile.type == PAAType::DXT5) {
        BlockDecompressImageDXT5(width, height, reinterpret_cast<unsigned char*>(DXTData->data()), reinterpret_cast<unsigned long*>(output.data()));
    } else if (texFile.type == PAAType::DXT1) {
        BlockDecompressImageDXT1(width, height, reinterpret_cast<unsigned char*>(DXTData->data()), reinterpret_cast<unsigned long*>(output.data()));
    }

    return { output, width, height };
}

void ModuleImageDirectory::LoadTextureToCache(std::string_view path) {


    auto found = imageCache.find(path);
    if (found != imageCache.end()) return; //already in cache

    auto tex = LoadRGBATexture(path);
    auto& [data, width, height] = tex;
    if (data.empty()) return;

    std::unique_lock lock(cacheLock);
    imageCache[std::string(path)] = tex;
}

void ModuleImageDirectory::OnGameMessage(const std::vector<std::string_view>& function,
                                         const std::vector<std::string_view>& arguments) {

    if (function.front() == "DoExport") {

        auto exportPtr = Util::GetArmaHostProcAddress("?ExportSVG@@YAXPEBD_N11111@Z");

        auto exportFunc = static_cast<void(*)(
            const char* name,
            bool drawLocationNames,
            bool drawGrid,
            bool drawCountlines,
            bool drawTreeObjects,
            bool drawMountainHeightpoints,
            bool simpleRoads
            )>(exportPtr);

        auto myDirectory = Util::GetCurrentDLLPath().parent_path();
        if (!std::filesystem::exists(myDirectory / "Maps"))
            std::filesystem::create_directory(myDirectory / "Maps");
        auto svgPath = myDirectory / "Maps" / std::filesystem::path(GModuleGameInfo.worldName + ".svg").replace_extension(".svg");
        exportFunc(svgPath.string().data(), true, true, true, false, false, true);

        auto msg = generateMapfileMessage(GModuleGameInfo.worldName+".svg").dump();
        for (auto& it : waitingForMapExport)
            it(msg);
        waitingForMapExport.clear();
    }
}

nlohmann::json ModuleImageDirectory::generateMapfileMessage(std::string_view path) {
    auto myDirectory = Util::GetCurrentDLLPath().parent_path();

    auto svgPath = myDirectory / "Maps" / std::filesystem::path(path).replace_extension(".svg");
    auto svgzPath = myDirectory / "Maps" / std::filesystem::path(path).replace_extension(".svgz");

    nlohmann::json msg;
    msg["cmd"] = { "ImgDir", "MapFile" };
    auto& args = msg["args"];

    if (std::filesystem::exists(svgzPath)) {
        args["name"] = svgzPath.filename().string();

        std::vector<char> buffer;
        buffer.resize(std::filesystem::file_size(svgzPath));
        std::ifstream fstr(svgzPath, std::ifstream::binary | std::ifstream::in);
        fstr.read(buffer.data(), buffer.size());

        args["data"] = base64_encode(std::string_view(buffer.data(), buffer.size()));
    } else if (std::filesystem::exists(svgPath)) {
        args["name"] = svgPath.filename().string();


        std::vector<char> buffer;
        buffer.resize(std::filesystem::file_size(svgPath));
        std::ifstream fstr(svgPath, std::ifstream::binary | std::ifstream::in);
        fstr.read(buffer.data(), buffer.size());

        args["data"] = base64_encode(std::string_view(buffer.data(), buffer.size()));
    } else {
        // It didn't work? try again?
        GGameManager.SendMessage("ImgDir.ReqExport", "");
        return {};
    }
    return msg;
}

void ModuleImageDirectory::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const NetworkMessageContext& context) {

    if (function[0] == "RequestTexture") {
        std::string_view path = arguments["path"];

        auto [data, width, height] = LoadRGBATexture(path);

        nlohmann::json msg;
        msg["cmd"] = {"ImgDir", "TextureFile"};
        auto& args = msg["args"];
        args["path"] = path;
        args["data"] = base64_encode(std::string_view(data.data(), data.size()));
        args["width"] = width;
        args["height"] = height;

        context.Reply(std::reference_wrapper(msg));
    } else if (function[0] == "RequestMapfile") {
        std::string_view path = arguments["name"];

        nlohmann::json msg = generateMapfileMessage(path);

        if (msg.is_null()) {
            waitingForMapExport.push_back([context](std::string_view res) {context.Reply(res); });
            return;
        }

        context.Reply(std::reference_wrapper(msg));
    }
}
