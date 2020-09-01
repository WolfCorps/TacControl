#pragma once
#include "Util/Util.hpp"




class GameMessage {
    std::vector<std::string> data;

public:
    GameMessage(const char* func, const char** argv, int argc) {

        data.reserve(argc + 1);
        data.emplace_back(func);
        arguments.reserve(argc);

        for (int i = 0; i < argc; ++i) {
            data.emplace_back(Util::trim(std::string_view(argv[i]), "\"")); //Check if " trim is needed
        }
        for (int i = 0; i < argc; ++i) {
            arguments.emplace_back(data[i + 1]);
        }

        function = Util::split(data.front(), '.');
    }

    std::string_view funcPopFront() {
        auto ret = function.front();
        function.erase(function.begin());
        return ret;
    }

    std::vector<std::string_view> function;
    std::vector<std::string_view> arguments;




};
