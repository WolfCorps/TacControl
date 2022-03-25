#include "script_component.hpp"

ADDON = false;

#include "XEH_PREP.hpp"

ADDON = true;

if (isDedicated) exitWith { call TC_main_fnc_Core_preInit; call TC_main_fnc_GPS_preInit; };

"TacControl" callExtension "preInit";

call TC_main_fnc_Core_preInit;
call TC_main_fnc_Radio_preInit;
call TC_main_fnc_GPS_preInit;
call TC_main_fnc_Marker_preInit;
call TC_main_fnc_Vehicle_preInit;
call TC_main_fnc_ACE_preInit;

["GameInfo.WorldLoaded", [worldName]] call TC_main_fnc_sendMessage;


GVAR(HandlerMap) = createHashMapFromArray [
    ["Core",    TC_main_fnc_Core_onMessage],
    ["Radio",   TC_main_fnc_Radio_onMessage],
    ["GPS",     TC_main_fnc_GPS_onMessage],
    ["Marker",  TC_main_fnc_Marker_onMessage],
    ["Vehicle", TC_main_fnc_Vehicle_onMessage],
    ["ACE",     TC_main_fnc_ACE_onMessage],
    ["ImgDir",  TC_main_fnc_exportTerrain] //There is only one possible command for ImgDir
];

addMissionEventHandler ["ExtensionCallback", {
    params ["_name", "_function", "_data"];

    if (_name != "TC") exitWith {};

    _function = _function splitString ".";
    _arguments = _data splitString endl;

    // We assume that a handler always exists, don't bother checking
    [_function select [1, 9999], _arguments] call (GVAR(HandlerMap) getOrDefault [_function#0, {}])
}];
