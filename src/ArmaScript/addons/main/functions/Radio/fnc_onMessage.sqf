#include "script_component.hpp"

params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "Transmit": {
            private _radioClass = _arguments#0;
            private _radioChannel = parseNumber (_arguments#1);
            private _startTransmit = _arguments#2 == "true";


            if (" " in _radioClass) then {
                private _splitClass = _radioClass splitString " ";
                _splitClass set [0, ((_splitClass select 0) call TC_main_fnc_Radio_getLRRadioByNetId) select 0];

                _radioClass = _splitClass;
            };



            [_radioClass, _radioChannel, _startTransmit] call TC_main_fnc_Radio_TFARTransmit;
        };


    };


};
