#include "script_component.hpp"

params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "UpdateTrackers": {
            call TC_main_fnc_GPS_sendTrackerUpdate;
        };
        case "SetTrackerName": {
            _arguments params ["_netId", "_newName"];

             missionNamespace setVariable ["TC_gt_"+_netId, _newName, true];
        };
    };
};
