#include "script_component.hpp"

params ["_netId"];

private _found = TC_TFAR_Radios findIf {
    if (_x isEqualType "") exitWith {false};
    (netId (_x select 0) == _netId)
};

if (_found == -1) exitWith {objNull};

TC_TFAR_Radios select _found;
