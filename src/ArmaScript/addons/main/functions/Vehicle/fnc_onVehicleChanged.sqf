#include "script_component.hpp"

params ["_unit", "_vehicle", "_oldVehicle"];

//systemChat str ["vecChan", _this];

#define EVENTHANDLERS \
    XX(Engine,TC_main_fnc_Vehicle_onEngineChanged) \
    XX(GetIn,TC_main_fnc_Vehicle_updatePassengers) \
    XX(GetOut,TC_main_fnc_Vehicle_updatePassengers) \
    XX(RopeAttach,TC_main_fnc_Vehicle_onSlingloadChanged) \
    XX(RopeBreak,TC_main_fnc_Vehicle_onSlingloadChanged) \

if (!isNull _oldVehicle) then {
#define XX(name,handler) \
    private _eh##name = _oldVehicle getVariable [QUOTE(DOUBLES(TC_Vehicle_EH,name)), -1]; \
    if (_eh##name != -1) then { \
        _oldVehicle removeEventHandler [QUOTE(name), _eh##name]; \
        _oldVehicle setVariable [QUOTE(DOUBLES(TC_Vehicle_EH,name)), nil]; \
    };
EVENTHANDLERS;
#undef XX
};

["VehAnim", GVAR(VehAnimInterestHandle)] call TC_main_fnc_Core_unregisterOnInterest;

TC_Vehicle_CurrentVec = _vehicle;

//Left vehicle
if (isNull _vehicle || _vehicle == _unit) exitWith {
    ["Vehicle.VecLeft", []] call TC_main_fnc_sendMessage;
};

//Assemble info
private _isDriver = (driver _vehicle isEqualTo player); //#TODO use ACE_Player instead?

//We only do most things if you are driver, controlling gear and such, currently just disable everything for non-driver cuz lazy
if !(_isDriver) exitWith {};


//Register eventhandlers
#define XX(name, handler) \
    _vehicle setVariable [QUOTE(DOUBLES(TC_Vehicle_EH,name)), _vehicle addEventHandler[QUOTE(name), handler]];

EVENTHANDLERS;
#undef XX

// We only want to update vehicle animations if there is actually interest for it.
//#TODO also only do the eventhandlers on interest?
GVAR(VehAnimInterestHandle) = ["VehAnim", 
    { // Triggered when interest is gained or already present
       
        _this set [0,
            [{
                TC_Vehicle_CurrentVec call TC_main_fnc_Vehicle_updateAnimSources;
            }, 0.2] call CBA_fnc_addPerFrameHandler
        ];
    }, 
    { // Triggered when interest is lost or handler is removed
        _this call CBA_fnc_removePerFrameHandler;
    },
    [0] // using our own args array to store the PFH handle
] call TC_main_fnc_Core_registerOnInterest;


private _vehicleCapabilities = [];

private _config = configOf _vehicle;


if (getNumber (_config >> "gearRetracting") == 1) then{
    _vehicleCapabilities append ["CanGear", 1];
};

_vehicleCapabilities append ["FuelCapacity", getNumber (_config >> "fuelCapacity")];
_vehicleCapabilities append ["FuelConsumptionRate", getNumber (_config >> "fuelConsumptionRate")];
_vehicleCapabilities append ["FuelConsumptionRate", getNumber (_config >> "fuelConsumptionRate")];
_vehicleCapabilities append ["Picture", getText (_config >> "picture")];
_vehicleCapabilities append ["DisplayName", getText (_config >> "displayName ")];

_vehicleCapabilities append ["CanLight", 1]; //#TODO if _config>>Reflectors contains a light

// _config >> slingLoadMaxCargoMass //#TODO use for sling loading to check mass
// _config >> slingLoadMemoryPoint
//#TODO slongLoadCargoMemoryPoints to check if a object supports to be slingloaded
// _config >> maxSpeed, should be max for animSource but should not use animsource
// _config >> picture, icon
//"cargoDoor"
//"sideDoors1"

//AnimationSources has entry with source="door" its a door, need to use animateDoor on it


//#TODO ACE Cargo support, button to airdrop cargo

["Vehicle.Update", ["CollisionLight", isCollisionLightOn _vehicle, "EngineOn", isEngineOn _vehicle, "LightOn", isLightOn _vehicle]] call TC_main_fnc_sendMessage;
_vehicle call TC_main_fnc_Vehicle_updatePassengers;
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
["Vehicle.VecEntered", [_isDriver]] call TC_main_fnc_sendMessage;

