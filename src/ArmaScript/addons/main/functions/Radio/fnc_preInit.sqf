#include "script_component.hpp"


//Add TFAR eventhandlers

private _evtOnTangent = {
    params ["_unit", "_radio", "_type", "_isAdditional", "_start"];

    //#TODO LR Radio support
    if (_radio isEqualType []) exitWith {};

    //private _currentChannel = if (_isAdditional) then {_radio call TFAR_fnc_getAdditionalSwChannel} else {_radio call TFAR_fnc_getSwChannel};

    //_radioChannel comes from TFAR's transmit function

    ["Radio.Transmit", [_radio, _radioChannel, _start]] call TC_main_fnc_sendMessage;
};



private _evtOnSWchannelSet = {
    params ["", "_radio", "", ""];

    //#TODO LR Radio support
    if (_radio isEqualType []) exitWith {};


    private _currentChannel = _radio call TFAR_fnc_getSwChannel;
    private _currentAltChannel = _radio call TFAR_fnc_getAdditionalSwChannel;

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

    ["Radio.RadioUpdate", [_radio, _currentChannel, _currentAltChannel, _channels]] call TC_main_fnc_sendMessage;
};

private _evtOnRadiosReceived = {
    params ["", "_radios"];

    {
        private _radio = _x;

        private _currentChannel = _radio call TFAR_fnc_getSwChannel;
        private _currentAltChannel = _radio call TFAR_fnc_getAdditionalSwChannel;

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

        ["Radio.RadioUpdate", [_radio, _currentChannel, _currentAltChannel, _channels]] call TC_main_fnc_sendMessage;
    } forEach _radios;
};

private _evtOnFrequencyChangedFromUI = {
    params ["", "_radio", "", ""];

    //#TODO LR Radio support
    if (_radio isEqualType []) exitWith {};


    private _currentChannel = _radio call TFAR_fnc_getSwChannel;
    private _currentAltChannel = _radio call TFAR_fnc_getAdditionalSwChannel;

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

    ["Radio.RadioUpdate", [_radio, _currentChannel, _currentAltChannel, _channels]] call TC_main_fnc_sendMessage;
};


["TFAR_event_OnTangent", _evtOnTangent] call CBA_fnc_addEventHandler;
["TFAR_event_OnSWchannelSet", _evtOnSWchannelSet] call CBA_fnc_addEventHandler;
["TFAR_event_OnRadiosReceived", _evtOnRadiosReceived] call CBA_fnc_addEventHandler;
["TFAR_event_OnFrequencyChangedFromUI", _evtOnFrequencyChangedFromUI] call CBA_fnc_addEventHandler;
