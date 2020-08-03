#pragma once
#include <string>
#include <vector>

#include <nlohmann/json.hpp>


//#TODO concepts


// SFINAE test
template <typename T>
class has_Serialize2 {
    typedef char one;
    typedef long two;

    template <typename C> static one test(char[sizeof(&C::Serialize)]);
    template <typename C> static two test(...);

public:
    enum { value = sizeof(test<T>(0)) == sizeof(char) };
};


template<typename Class>
class has_Serialize {
    typedef char(&yes)[2];

    template<typename> struct Exists; // <--- changed

    template<typename V>
    static yes CheckMember(Exists<decltype(&V::Serialize)>*); // <--- changed (c++11)
    template<typename>
    static char CheckMember(...);

public:
    static const bool value = (sizeof(CheckMember<Class>(0)) == sizeof(yes));
};




class JsonArchive {
public:
    JsonArchive() : pJson(new nlohmann::json()), isReading(false) {}
    JsonArchive(nlohmann::json& js) : pJson(&js), isReading(true) {}
    ~JsonArchive() { if (!isReading) delete pJson; }
    bool reading() const { return isReading; }
    nlohmann::json* getRaw() const { return pJson; }
    std::string to_string();

    //typename std::enable_if<has_Serialize<Type>::value || has_Serialize<typename Type::baseType>::value>::type

    void Serialize(const char* key, JsonArchive& ar) {
        if (isReading) __debugbreak(); //not implemented
        (*pJson)[key] = *ar.pJson;
    }

    template <class Type>
    typename std::enable_if<has_Serialize<Type>::value>::type
        Serialize(const char* key, const std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        _array.array({});
        if (isReading) {
            if (!_array.is_array()) __debugbreak();
            for (auto& it : _array) {
                __debugbreak(); //#TODO AutoArray pushback
            }
        }
        else {
            value.for_each([&_array](const Type& value) {
                JsonArchive element;
                value.Serialize(element);
                _array.push_back(*element.pJson);
                });
        }
    }

    template <class Type>
    typename std::enable_if<!has_Serialize<Type>::value>::type
        Serialize(const char* key, const std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        _array.array({});
        if (isReading) {
            if (!_array.is_array()) __debugbreak();
            for (auto& it : _array) {
                __debugbreak(); //#TODO AutoArray pushback
            }
        }
        else {
            value.for_each([&_array](const Type& value) {
                JsonArchive element;
                ::Serialize(value, element);
                _array.push_back(*element.pJson);
                });
        }
    }



    template <class Type>
    typename std::enable_if<has_Serialize<Type>::value || has_Serialize<typename Type::baseType>::value>::type
        Serialize(const char* key, std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        if (isReading) {
            if (!_array.is_array()) __debugbreak();
            for (auto& it : _array) {
                __debugbreak(); //#TODO AutoArray pushback
            }
        }
        else {
            value.for_each([&_array](Type& value) {
                JsonArchive element;
                value.Serialize(element);
                _array.push_back(*element.pJson);
                });
        }
    }



    template <class Type>
    typename std::enable_if<!has_Serialize<Type>::value && !has_Serialize<typename Type::baseType>::value>::type
        Serialize(const char* key, std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        if (isReading) {
            if (!_array.is_array()) __debugbreak();
            for (auto& it : _array) {
                __debugbreak(); //#TODO AutoArray pushback
            }
        }
        else {
            for (auto&& it : value) {
                JsonArchive element;
                ::Serialize(*it, element);
                _array.push_back(*element.pJson);
            }
        }
    }

    template <class Type>
    typename std::enable_if<!has_Serialize<Type>::value>::type
        Serialize(const char* key, std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        if (isReading) {
            if (_array.is_array()) {
                for (auto& it : _array) {
                    if constexpr (std::is_convertible_v<Type, r_string>)
                        value.emplace_back(r_string(it.get<std::string>()));
                    else
                        value.push_back(it);
                }
            }
        }
        else {
            for (Type& it : value) {
                if constexpr (std::is_convertible_v<Type, r_string>)
                    _array.push_back(it.data());
                else
                    _array.push_back(it);
            }

        }
    }

    template <class Type>
    typename std::enable_if<has_Serialize<Type>::value>::type
        Serialize(const char* key, std::vector<Type>& value) {
        auto& _array = (*pJson)[key];
        if (isReading) {
            if (_array.is_array()) {
                for (auto& it : _array) {
                    auto iterator = value.emplace(value.end(), Type());
                    JsonArchive tmpAr(it);
                    iterator->Serialize(tmpAr);
                }
            }
        }
        else {
            for (Type& it : value) {
                JsonArchive element;
                it.Serialize(element);
                _array.push_back(*element.pJson);
            }
        }
    }

    //Generic serialization. Calls Type::Serialize
    template <class Type>
    typename std::enable_if<has_Serialize<Type>::value>::type
        Serialize(const char* key, Type& value) {
        if (isReading) {
            __debugbreak(); //not implemented
        }
        else {
            JsonArchive element;
            value.Serialize(element);
            (*pJson)[key] = *element.pJson;
        }
    }


    template <class Type>
    typename std::enable_if<!has_Serialize<Type>::value && !std::is_convertible<Type, std::nullptr_t>::value>::type
        Serialize(const char* key, Type& value) {
        if (isReading) {
            value = (*pJson)[key].get<Type>();
        }
        else {
            (*pJson)[key] = value;
        }
    }

    template <class Type>
    void writeOnly(const char* key, Type& value) {
        if (isReading) {
            __debugbreak();
        }
        else {
            (*pJson)[key] = value;
        }
    }


    template <class Type>
    typename std::enable_if<has_Serialize<Type>::value && !std::is_convertible<Type, std::nullptr_t>::value>::type
        Serialize(const char* key, const Type& value) {
        if (isReading) {
            __debugbreak(); //not possible
        }
        else {
            JsonArchive element;
            value.Serialize(element);
            (*pJson)[key] = *element.pJson;
        }
    }

    template <class Type>
    typename std::enable_if<!has_Serialize<Type>::value && !std::is_convertible<Type, std::nullptr_t>::value>::type
        Serialize(const char* key, const Type& value) {
        if (isReading) {
            __debugbreak(); //not possible
        }
        else {
            (*pJson)[key] = value;
        }
    }

    void Serialize(const char* key, std::string& value);
    void Serialize(const char* key, const std::string& value);

    //************************************
    //serializeFunction - Function that is called for every element in the Array
    //************************************
    template <class Type, class Func>
    void Serialize(const char* key, std::vector<Type>& value, Func& serializeFunction) {
        auto& _array = (*pJson)[key];
        if (isReading) {
            if (!_array.is_array()) __debugbreak();
            for (auto& it : _array) {
                __debugbreak(); //#TODO AutoArray pushback
            }
        }
        else {
            value.forEach([&_array, &serializeFunction](auto&& value) {
                JsonArchive element;
                serializeFunction(element, value);
                _array.push_back(*element.pJson);
                });
        }
    }

    //************************************
    //serializeFunction - Function that is called for every element in the Array
    //************************************
    template <class Type, class Func>
    void Serialize(const char* key, const std::vector<Type>& value, Func&& serializeFunction) {
        if (isReading) {
            __debugbreak(); //not possible
        }
        else {
            auto& _array = (*pJson)[key];
            value.for_each([&_array, &serializeFunction](auto&& value) {
                JsonArchive element;
                serializeFunction(element, value);
                _array.push_back(*element.pJson);
                });
        }
    }

    template <class Type>
    void Serialize(const char* key, const std::initializer_list<Type>& value) {
        auto& _array = (*pJson)[key];
        for (auto& it : value) {
            _array.push_back(it);
        }
    }

private:
    nlohmann::json* pJson;
    bool isReading;
};

class Serialize {
public:
    Serialize();
    ~Serialize();
};


static void from_json(const json& j, game_instruction& in) { }