#include "ModuleImageDirectory.hpp"
#include <ranges>
#include <algorithm>
#include <pbo.hpp>
#include <TextureFile.hpp>
#include "Util/Util.hpp"
#include <base64.h>
#include <s3tc.h>
#include <minilzo.h>

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

}

void ModuleImageDirectory::ModulePostInit() {
    LoadPboPrefixes();
}

decltype(ModuleImageDirectory::pboWithPrefixes)::iterator ModuleImageDirectory::FindPboByFilepath(std::string_view path) {
    path = Util::trim(path, "\\");

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
  
    auto& fs = preader.getFileBuffer(*foundFile);
    std::istream source(&fs);

    std::vector<char> result;
    result.resize(foundFile->original_size);

    source.read(result.data(), foundFile->original_size);
    return result;
}

template<typename CharT, typename TraitsT = std::char_traits<CharT> >
class vectorwrapbuf : public std::basic_streambuf<CharT, TraitsT> {
public:
    vectorwrapbuf(std::vector<CharT>& vec) {
        setg(vec.data(), vec.data(), vec.data() + vec.size());
    }

    std::basic_streambuf<CharT, TraitsT>::pos_type seekoff(
        std::basic_streambuf<CharT, TraitsT>::off_type off, std::ios_base::seekdir dir, std::ios_base::openmode) override {

        auto first = eback();


        if (dir == 0)
            setg(first, first + off, egptr());
        else if (dir == 1)
            setg(first, gptr() + off, egptr());

        return std::streampos(std::distance(first, gptr()));
    }


};

std::vector<char> ModuleImageDirectory::LoadRGBATexture(std::string_view path) {

    auto found = imageCache.find(path);
    if (found != imageCache.end()) {
        return found->second;
    }

    auto paaData = LoadFileToBuffer(path);
    if (paaData.empty()) return {};

    TextureFile texFile;
    texFile.doLogging = false;

    vectorwrapbuf<char> databuf(paaData);
    std::istream is(&databuf);
    texFile.readFromStream(is);

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

    return output;
}

void ModuleImageDirectory::LoadTextureToCache(std::string_view path) {

    auto tex = LoadRGBATexture(path);
    if (tex.empty()) return;

    imageCache[std::string(path)] = tex;
}

void ModuleImageDirectory::OnGameMessage(const std::vector<std::string_view>& function,
                                         const std::vector<std::string_view>& arguments) {

}

void ModuleImageDirectory::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {

    if (function[0] == "RequestTexture") {
        std::string_view path = arguments["path"];

        auto tex = LoadRGBATexture(path);

        nlohmann::json retDoc;

        nlohmann::json msg;
        msg["cmd"] = {"ImgDir", "TextureFile"};
        auto& args = msg["args"];
        args["path"] = path;
        args["data"] = base64_encode(std::string_view(tex.data(), tex.size()));

        replyFunc(msg.dump());
    }
}