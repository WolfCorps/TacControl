#pragma once
#include <stdexcept>
#include <string_view>
#include <vector>
#include <cmath>
#include <filesystem>

#include <fmt/format.h>

class JsonArchive;
using namespace std::string_view_literals;

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
        if (begin == std::string::npos)
            return std::string_view();
        if (end == std::string::npos)
            end = string.length() - 1;
        return string.substr(begin, end - begin + 1);
    }


    static std::string_view trim(std::string_view string) {
        /*
         * Trims tabs and spaces on either side of the string.
         */
        if (string.empty()) return "";

        return trim(string, "\t "sv);
    }

    static std::string toLower(std::string_view string) {
        std::string ret(string);
        std::transform(ret.begin(), ret.end(), ret.begin(), ::tolower);

        return ret;
    }

    static bool stringEqualsCaseInsensitive(std::string_view l, std::string_view r) {
        if (l.length() != r.length()) return false;

        return std::equal(l.cbegin(), l.cend(),
            r.cbegin(), r.cend(),
            [](unsigned char l, unsigned char r) {
                return l == r || tolower(l) == tolower(r);
            });
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


    struct string_hash {
        using is_transparent = void;
        [[nodiscard]] size_t operator()(std::string_view txt) const { return std::hash<std::string_view>{}(txt); }
        [[nodiscard]] size_t operator()(const std::string& txt) const { return std::hash<std::string>{}(txt); }
    };


    std::vector<std::filesystem::path> GenerateLoadedPBOList();

    std::filesystem::path GetCurrentDLLPath();

    void* GetArmaHostProcAddress(std::string name);

    bool IsDebuggerPresent();

    void BreakToDebuggerIfPresent();

}

class Vector2D {
public:
    Vector2D() = default;
    Vector2D(float x, float y);
    Vector2D(std::string_view coordinateString);
    //Vector2D(Vector2D&& vec) noexcept : m_x(vec.m_x), m_y(vec.m_y) {};
    //Vector2D(const Vector2D& vec) = default;

    std::tuple<float, float> get() const; //#TODO instead of using get.. how about operator[] ?
    float& operator[](int offs) {
        switch (offs) {
        case 0: return m_x;
        case 1: return m_y;
        default: throw std::out_of_range("Index out of range. A 3D vector only has 3 values.");
        }
    }
    std::string toString() const;

    void Serialize(JsonArchive& ar);
protected:
    float m_x = 0.f;
    float m_y = 0.f;
};

class Vector3D {
public:
    Vector3D() = default;
    Vector3D(float x, float y, float z);
    Vector3D(const std::vector<float>& vec);
    Vector3D(std::string_view coordinateString);
    Vector3D(Vector3D&& vec) noexcept : m_x(vec.m_x), m_y(vec.m_y), m_z(vec.m_z) {};
    Vector3D(const Vector3D& vec) = default;

    std::tuple<float, float, float> get() const; //#TODO instead of using get.. how about operator[] ?
    float& operator[](int offs) {
        switch (offs) {
        case 0: return m_x;
        case 1: return m_y;
        case 2: return m_z;
        default: throw std::out_of_range("Index out of range. A 3D vector only has 3 values.");
        }
    }
    std::string toString() const;


    float length() const;
    float lengthSqr() const;
    float dotProduct(const Vector3D& other) const;
    Vector3D crossProduct(const Vector3D& other) const;
    Vector3D normalized() const;
    bool isNull() const;
    Vector3D operator*(float multiplier) const {
        return{ m_x * multiplier,m_y * multiplier ,m_z * multiplier };
    }
    Vector3D& operator=(const Vector3D& other) = default;
    Vector3D operator-(const Vector3D& other) const;
    Vector3D operator+(const Vector3D& other) const;
    bool operator< (const Vector3D& other) const;
    bool operator== (const Vector3D& other) const;
    Vector3D operator/(float div) const;

    void Serialize(JsonArchive& ar);

protected:
    float m_x = 0.f;
    float m_y = 0.f;
    float m_z = 0.f;
};
