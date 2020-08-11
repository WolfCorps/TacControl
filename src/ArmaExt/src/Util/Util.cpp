#include "Util.hpp"

#include "Networking/Serialize.hpp"

float fast_invsqrt(float number) {
    //Quake is awesome!

    const auto threehalfs = 1.5F;
    const auto x2 = number * 0.5F;
    auto y = number;
    auto i = *reinterpret_cast<long*>(&y);
    i = 0x5f3759df - (i >> 1);
    y = *reinterpret_cast<float*>(&i);
    y = y * (threehalfs - (x2 * y * y));
    return y;
}

Vector3D::Vector3D(float x, float y, float z) :m_x(x), m_y(y), m_z(z) {

}

Vector3D::Vector3D(const std::vector<float>& vec) {
    switch (vec.size()) {
    case 2:m_x = vec.at(0); m_y = vec.at(1); break;
    case 3:
    case 4: //Old overrides with TF_fnc_position may return 4 elements but we only want 3
        m_x = vec.at(0); m_y = vec.at(1); m_z = vec.at(2); break;
    }
}

Vector3D::Vector3D(std::string_view coordinateString) {
    coordinateString = Util::trim(coordinateString, "[]");
    if (coordinateString.length() < 3)
        return; //Fail
    std::vector<std::string_view> coords; coords.reserve(3);
    Util::split(coordinateString, ',', coords);
    switch (coords.size()) {
    case 2:
        m_x = Util::parseArmaNumber(coords.at(0));
        m_y = Util::parseArmaNumber(coords.at(1));
        break;
    case 3:
    case 4: //Old overrides with TF_fnc_position may return 4 elements but we only want 3
        m_x = Util::parseArmaNumber(coords.at(0));
        m_y = Util::parseArmaNumber(coords.at(1));
        m_z = Util::parseArmaNumber(coords.at(2));
        break;
        //default Fail
    }
}

bool Vector3D::operator==(const Vector3D& other) const {
    return m_x == other.m_x && m_y == other.m_y && m_z == other.m_z;
}

Vector3D Vector3D::operator-(const Vector3D& other) const {
    return Vector3D(
        m_x - other.m_x,
        m_y - other.m_y,
        m_z - other.m_z);
}

Vector3D Vector3D::operator+(const Vector3D& other) const {
    return Vector3D(
        m_x + other.m_x,
        m_y + other.m_y,
        m_z + other.m_z);
}

bool Vector3D::operator<(const Vector3D& other) const {
    //Is this of any use?
    return length() < other.length();
}

Vector3D Vector3D::operator/(float div) const {
    return{ m_x / div,m_y / div,m_z / div };
}

void Vector3D::Serialize(JsonArchive& ar) {
    auto& js = *ar.getRaw();
    js.push_back(m_x);
    js.push_back(m_y);
    js.push_back(m_z);
}

std::tuple<float, float, float> Vector3D::get() const {
    return{ m_x ,m_y ,m_z };
}

float Vector3D::length() const {
    return sqrt(m_x * m_x + m_y * m_y + m_z * m_z);
}

float Vector3D::lengthSqr() const {
    return m_x * m_x + m_y * m_y + m_z * m_z;
}

float Vector3D::dotProduct(const Vector3D& other) const {
    return m_x * other.m_x + m_y * other.m_y + m_z * other.m_z;
}

Vector3D Vector3D::crossProduct(const Vector3D& other) const
{
    return {
        m_y * other.m_z - m_z * other.m_y,
        m_z * other.m_x - m_x * other.m_z,
        m_x * other.m_y - m_y * other.m_x
    };
}

Vector3D Vector3D::normalized() const {
    Vector3D other;
    const auto len = fast_invsqrt(length());
    if (len != 0) {
        other.m_x = m_x * len;
        other.m_y = m_y * len;
        other.m_z = m_z * len;
    }
    return other;
}

bool Vector3D::isNull() const {
    //Is initialized. Used to check if FromString was successful
    //May optimize this by storing whether FromString was success but fpOps are fast enough
    return m_x == 0.f && m_y == 0.f && m_z == 0.f;
}

std::string Vector3D::toString() const {
    //auto [x, y, z] = get();
    //std::stringstream out;
    //out << "[" << x << "," << y << "," << z << "]";
    //return out.str();
    return "";
}
