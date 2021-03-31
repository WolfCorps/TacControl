#include "script_component.hpp"

params ["_netId"];

private _found = TC_GPSTrackers findIf {
        (_x call BIS_fnc_netId) == _netId
};

if (_found == -1) exitWith {objNull};

TC_GPSTrackers select _found;
