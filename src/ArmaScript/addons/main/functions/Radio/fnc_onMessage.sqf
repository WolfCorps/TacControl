#include "script_component.hpp"

params ["_function", "_arguments"];





#define CONVBOOL_R(x) (x == "true")
#define CONVNUM_R(x) (parseNumber x)
#define CONVRADIO_R(x) \
    if (" " in x) then { \
        private _splitClass = x splitString " "; \
        _splitClass set [0, ((_splitClass select 0) call TC_main_fnc_Radio_getLRRadioByNetId) select 0]; \
        _splitClass; \
    } else {x}

#define CONVRADIO(x) x = CONVRADIO_R(x)
#define CONVBOOL(x) x = CONVBOOL_R(x)
#define CONVNUM(x) x = CONVNUM_R(x)


#define IS_LR(x) (x isEqualType [])

if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "Transmit": {
            _arguments params ["_radio", "_channel", "_startTransmit"];
            CONVRADIO(_radio);
            CONVNUM(_channel);
            CONVBOOL(_startTransmit);

            [_radio, _channel, _startTransmit] call TC_main_fnc_Radio_TFARTransmit;
        };
        case "SetStereo":{
            _arguments params ["_radioClass", "_isMain", "_mode"];
            CONVRADIO(_radio);
            CONVBOOL(_isMain);
            CONVNUM(_mode);

            private _func = [[TFAR_fnc_setAdditionalSwStereo, TFAR_fnc_setAdditionalLrStereo], [TFAR_fnc_setSwStereo, TFAR_fnc_setLrStereo]] select _isMain select IS_LR(_radio);

            [_radio, _mode] call _func;
        };
        case "SetSpeaker":{
            _arguments params ["_radioClass", "_enabled"];
            CONVRADIO(_radio);
            CONVBOOL(_enabled);

            private _getFunc = [TFAR_fnc_getSwSpeakers, TFAR_fnc_getLrSpeakers] select IS_LR(_radio);
            if ([_radio] call _getFunc != _enabled) then {
                private _func = [TFAR_fnc_setSwSpeakers, TFAR_fnc_setLrSpeakers] select IS_LR(_radio);
                [_radio] call _func;
            };
        };
        case "SetFrequency":{
            _arguments params ["_radio", "_channel", "_freq"];
            CONVRADIO(_radio);
            CONVNUM(_channel);

            [_radio, _channel + 1, _freq] call TFAR_fnc_setChannelFrequency;
        };
        case "SetVolume":{
            _arguments params ["_radio", "_volume"];
            CONVRADIO(_radio);
            CONVNUM(_volume);

            private _func = [TFAR_fnc_setSwVolume, TFAR_fnc_setLrVolume] select IS_LR(_radio);

            [_radio, _volume] call _func;
        };
        case "SetChannel":{
            _arguments params ["_radio", "_isMain", "_channel"];
            CONVRADIO(_radio);
            CONVBOOL(_isMain);
            CONVNUM(_channel);

            private _func = [[TFAR_fnc_setAdditionalSwChannel, TFAR_fnc_setAdditionalLrChannel], [TFAR_fnc_setSwChannel, TFAR_fnc_setLrChannel]] select _isMain select IS_LR(_radio);

            [_radio, _channel] call _func;
        };
    };


};
