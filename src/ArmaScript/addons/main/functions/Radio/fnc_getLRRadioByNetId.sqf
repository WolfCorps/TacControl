#include "script_component.hpp"

params ["_netId"];

private _found = TC_TFAR_Radios findIf {
    if (_x isEqualType []) then {
        (netId (_x select 0) == _netId)
    } else {
        false
    }
};

if (_found == -1) exitWith {objNull};

TC_TFAR_Radios select _found;
