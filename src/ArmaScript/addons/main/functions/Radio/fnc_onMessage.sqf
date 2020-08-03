params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "Transmit": {
            private _radioClass = _arguments#0;
            private _radioChannel = parseNumber (_arguments#1);
            private _startTransmit = _arguments#2 == "true";

            [_radioClass, _radioChannel, _startTransmit] call TC_main_fnc_Radio_TFARTransmit;
        };


    };


};
