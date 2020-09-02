#include "script_component.hpp"

params ["_radio"];

TC_TFAR_Radios pushBackUnique _radio;
private _isLR = _radio isEqualType [];
([
    [
        TFAR_fnc_getSwChannel,
        TFAR_fnc_getAdditionalSwChannel,
        TFAR_fnc_getSwStereo,
        TFAR_fnc_getAdditionalSwStereo,
        TFAR_fnc_getSwVolume,
        TFAR_fnc_getSwSpeakers
    ],
    [
        TFAR_fnc_getLrChannel,
        TFAR_fnc_getAdditionalLrChannel,
        TFAR_fnc_getLrStereo,
        TFAR_fnc_getAdditionalLrStereo,
        TFAR_fnc_getLrVolume,
        TFAR_fnc_getLrSpeakers
    ]
] select _isLR) params ["_getChannel", "_getAdditionalChannel", "_getStereo", "_getAdditionalStereo", "_getVolume", "_getSpeaker"];


private _currentChannel = [_radio] call _getChannel;
private _currentAltChannel = [_radio] call _getAdditionalChannel;

private _channels = [];
_channels pushBack ([_radio, 1] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 2] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 3] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 4] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 5] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 6] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 7] call TFAR_fnc_getChannelFrequency);
_channels pushBack ([_radio, 8] call TFAR_fnc_getChannelFrequency);

_channels = _channels joinString toString[10]; // "\n"

private _stereo = [_radio] call _getStereo;
private _addStereo = [_radio] call _getAdditionalStereo;
private _volume = [_radio] call _getVolume;
private _speaker = [_radio] call _getSpeaker;


["Radio.RadioUpdate", [TRANSFORM_LR_RADIO_TO_EXT(_radio), _currentChannel, _currentAltChannel, _channels, _stereo, _addStereo, _volume, _speaker]] call TC_main_fnc_sendMessage;
