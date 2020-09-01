#include "script_component.hpp"

params ["_unit", "_vehicle", "_oldVehicle"];

systemChat str ["vecChan", _this];

#define EVENTHANDLERS \
    XX(Engine,TC_main_fnc_Vehicle_onEngineChanged) \
    XX(GetIn,TC_main_fnc_Vehicle_updatePassengers) \
    XX(GetOut,TC_main_fnc_Vehicle_updatePassengers) \

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


[TC_Vehicle_PFH] call CBA_fnc_removePerFrameHandler;

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

TC_Vehicle_PFH = [{
     TC_Vehicle_CurrentVec call TC_main_fnc_Vehicle_updateAnimSources;
}, 0.05] call CBA_fnc_addPerFrameHandler;



["Vehicle.Update", ["CollisionLight", isCollisionLightOn _vehicle, "EngineOn", isEngineOn _vehicle, "LightOn", isLightOn _vehicle]] call TC_main_fnc_sendMessage;
_vehicle call TC_main_fnc_Vehicle_updatePassengers;
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
["Vehicle.VecEntered", [_isDriver]] call TC_main_fnc_sendMessage;

