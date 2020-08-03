#pragma once
#include <string_view>
#include <vector>

namespace Util
{
    static std::vector<std::string_view>& split(std::string_view s, char delim, std::vector<std::string_view>& elems) {
        std::string::size_type lastPos = 0;
        const std::string::size_type length = s.length();

        while (lastPos < length + 1) {
            std::string::size_type pos = s.find_first_of(delim, lastPos);
            if (pos == std::string::npos) {
                pos = length;
            }

            //if (pos != lastPos || !trimEmpty)
            elems.emplace_back(s.data() + lastPos, pos - lastPos);

            lastPos = pos + 1;
        }

        return elems;
    }


    static std::vector<std::string_view> split(std::string_view s, char delim) {
        std::vector<std::string_view> elems;
        split(s, delim, elems);
        return elems;
    }

    static std::string_view trim(std::string_view string, std::string_view trimChars) {
        if (string.empty()) return "";

        auto begin = string.find_first_not_of(trimChars);
        auto end = string.find_last_not_of(trimChars);
        return string.substr(begin, end - begin + 1);
    }


    static std::string_view trim(std::string_view string) {
        /*
         * Trims tabs and spaces on either side of the string.
         */
        if (string.empty()) return "";

        return trim(string, "\t "sv);
    }

    static float parseArmaNumber(std::string_view armaNumber) {
        return static_cast<float>(std::strtof(armaNumber.data(), nullptr));
    }
    static int parseArmaNumberToInt(std::string_view armaNumber) {
        return static_cast<int>(std::round(parseArmaNumber(armaNumber)));
    }
    static bool isTrue(std::string_view string) {
        if (string.length() != 4)//small speed optimization
            return string.length() == 1 && string.at(0) == '1';
        return string == "true";
    }


}



