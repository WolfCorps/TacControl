#include "Serialize.hpp"



Serialize::Serialize() {

}


Serialize::~Serialize() {}

std::string JsonArchive::to_string() {
    return pJson->dump(); // keep it all on one line, no prettifying or there will be no way for clients to separate the objects
}

void JsonArchive::Serialize(const char* key, const std::string& value) {
    if (isReading) __debugbreak(); //can't read this
    (*pJson)[key] = value.data();
}

void JsonArchive::Serialize(const char* key, std::string& value) {
    if (isReading) {
        (*pJson).at(key).get_to(value);
    } else {
        (*pJson)[key] = value.data();
    }
}
