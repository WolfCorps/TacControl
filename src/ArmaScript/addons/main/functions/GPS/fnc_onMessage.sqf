params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "UpdateTrackers": {
            call TC_main_fnc_GPS_sendTrackerUpdate;
        };


    };


};
