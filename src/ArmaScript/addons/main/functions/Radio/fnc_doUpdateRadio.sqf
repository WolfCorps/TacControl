#include "script_component.hpp"

params ["_radio"];

TC_TFAR_Radios pushBackUnique _radio;
private _isLR = _radio isEqualType [];
([
    [
        TFAR_fnc_getSwChannel,
        TFAR_fnc_getAdditionalSwChannel
    ],
    [
        TFAR_fnc_getLrChannel,
        TFAR_fnc_getAdditionalLrChannel
    ]
] select _isLR) params ["_getChannel", "_getAdditionalChannel"];


private _currentChannel = _radio call _getChannel;
private _currentAltChannel = _radio call _getAdditionalChannel;

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

["Radio.RadioUpdate", [TRANSFORM_LR_RADIO_TO_EXT(_radio), _currentChannel, _currentAltChannel, _channels]] call TC_main_fnc_sendMessage;
