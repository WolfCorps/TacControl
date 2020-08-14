#include "script_component.hpp"

ADDON = false;

#include "XEH_PREP.hpp"

ADDON = true;

if (isDedicated) exitWith { call TC_main_fnc_GPS_preInit; };

"TacControl" callExtension "preInit";

call TC_main_fnc_Radio_preInit;
call TC_main_fnc_GPS_preInit;
call TC_main_fnc_Marker_preInit;

["GameInfo.WorldLoaded", [worldName]] call TC_main_fnc_sendMessage;

addMissionEventHandler ["ExtensionCallback", {
    params ["_name", "_function", "_data"];

    if (_name != "TC") exitWith {};

    _function = _function splitString ".";
    _arguments = _data splitString endl;

    if (_function#0 == "Radio") then {
        _function = _function select [1, 9999];
        [_function, _arguments] call TC_main_fnc_Radio_onMessage;
    };
    if (_function#0 == "GPS") then {
        _function = _function select [1, 9999];
        [_function, _arguments] call TC_main_fnc_GPS_onMessage;
    };
    if (_function#0 == "Marker") then {
        _function = _function select [1, 9999];
        [_function, _arguments] call TC_main_fnc_Marker_onMessage;
    };
}];
