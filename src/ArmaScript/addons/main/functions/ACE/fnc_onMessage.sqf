#include "script_component.hpp"

params ["_function", "_arguments"];



if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "Detonate": {
            _arguments params ["_detonator", "_expList"];

            private _range = getNumber (configFile >> "CfgWeapons" >> _detonator >> "ace_explosives_range");

            private _explosives = [player] call ACE_explosives_fnc_getPlacedExplosives;


            _explosives = _explosives select {((_x select 0) call BIS_fnc_netId) in _expList};

            [player, _range, _explosives, _detonator]  call ACE_explosives_fnc_detonateExplosiveAll;
        };

    };
};
