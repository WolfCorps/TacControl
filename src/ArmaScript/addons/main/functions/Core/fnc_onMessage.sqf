#include "script_component.hpp"

params ["_function", "_arguments"];


if (_function#0 == "UpdateInterest") then {
    _arguments params ["_interestName", "_newState"];
    _newState = _newState == "true";

    [_interestName, _newState] call TC_main_fnc_Core_fireInterestHandlers;
};
