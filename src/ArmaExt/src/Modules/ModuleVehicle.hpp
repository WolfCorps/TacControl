#pragma once
#include <unordered_set>
#include "Util/Module.hpp"
#include "Util/Thread.hpp"
#include "Util/Util.hpp"


class JsonArchive;


// XX(name, type, default)
#define VEHICLE_PROPERTIES(XX) \
    XX(EngineOn, bool, false) \
    XX(AnimAltBaro, float, 0.f) \
    XX(AnimAltRadar, float, 0.f) \
    XX(AnimGear, float, 0.f) \
    XX(AnimHorizonBank, float, 0.f) \
    XX(AnimHorizonDive, float, 0.f) \
    XX(AnimVertSpeed, float, 0.f) \
    XX(AnimFuel, float, 0.f) \
    XX(AnimEngineTemp, float, 0.f) \
    XX(AnimRpm, float, 0.f) \
    XX(AnimSpeed, float, 0.f) \
    XX(AnimRotorHFullyDestroyed, float, 0.f) \
    XX(AnimTailRotorHFullyDestroyed, float, 0.f) \
    XX(AnimActiveSensorsOn, float, 0.f) \
    XX(AutoHover, bool, false) \
    XX(LightOn, bool, false) \
    XX(CollisionLight, bool, false) \
    XX(CanGear, bool, false) \
    XX(FuelCapacity, float, 0.f) \
    XX(FuelConsumptionRate, float, 0.f) \
    XX(Picture, std::string, std::string{}) \
    XX(DisplayName, std::string, std::string{}) \
    XX(CanLight, bool, false) \


class IVehicleProperty {
public:
    IVehicleProperty(std::string_view name) : name(name) {}
    virtual ~IVehicleProperty() = default;
    std::string name;
    virtual void Set(std::string_view newVal) = 0;
    virtual void Serialize(JsonArchive& ar) = 0;
};

struct VehiclePropertyEqual {
    using is_transparent = void;
    constexpr bool operator()(const std::unique_ptr<IVehicleProperty>& l, const std::unique_ptr<IVehicleProperty>& r) const {
        return static_cast<std::string_view>(l->name) == r->name;
    }
    constexpr bool operator()(const std::unique_ptr<IVehicleProperty>& l, const std::string_view& r) const {
        return l->name == r;
    }
    constexpr bool operator()(const std::string_view& l, const std::unique_ptr<IVehicleProperty>& r) const {
        return l == r->name;
    }
};

struct VehiclePropertyHasher {
    using is_transparent = void;
    std::size_t operator()(const std::unique_ptr<IVehicleProperty>& k) const {
        return std::hash<std::string_view>()(k->name);
    }
    std::size_t operator()(const std::string_view& k) const {
        return std::hash<std::string_view>()(k);
    }
};

template<typename Type>
class VehicleProperty : public IVehicleProperty {
    Type value;
public:
    VehicleProperty(std::string_view name, Type defaultValue) : IVehicleProperty(name), value(defaultValue) {}
    Type Get() const { return value; }
    void Set(std::string_view newVal) override;
    void Set(Type newVal) { value = newVal; }
    void Serialize(JsonArchive& ar) override;
};

struct VehicleCrewMember {
    std::string name;
    std::string role; //#TODO enum
    int cargoIndex{};
    //turretPath
    //bool personTurret
    void Serialize(JsonArchive& ar);

    VehicleCrewMember() {}
    VehicleCrewMember(std::string_view name, std::string_view role, int cargoIndex) : name(name), role(role), cargoIndex(cargoIndex) {}
};


class ModuleVehicle : public ThreadQueuePeriodic, public IMessageReceiver, public IStateHolder {
    std::unordered_set<std::unique_ptr<IVehicleProperty>, VehiclePropertyHasher, VehiclePropertyEqual> vehicleProperties;
    std::vector<VehicleCrewMember> vehicleCrew;
    bool isInVehicle = false;

    void UpdateProperty(std::string_view name, std::string_view value);

public:
    ModuleVehicle();

    //ThreadQueue
    void ModulePostInit() override { SetThreadName("TacControl_Vehicle"); }

    //IMessageReceiver
    std::string_view GetMessageReceiverName() override { return "Vehicle"sv; }
    void OnGameMessage(const std::vector<std::string_view>& function,
        const std::vector<std::string_view>& arguments) override;

    void OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) override;

    //IStateHolder
    std::string_view GetStateHolderName() override { return "Vehicle"sv; }
    void SerializeState(JsonArchive& ar) override;


    void SetDefaultProperties();
};

inline ModuleVehicle GModuleVehicle;
