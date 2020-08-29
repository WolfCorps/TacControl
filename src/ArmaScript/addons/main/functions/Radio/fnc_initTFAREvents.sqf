#include "script_component.hpp"

private _evtOnTangent = {
    params ["_unit", "_radio", "_type", "_isAdditional", "_start"];

    private _currentChannel = 0;

    if (isNil "_radioChannel") then {//Not from our transmit code

        if (_radio isEqualType []) then {
            _currentChannel = if (_isAdditional) then {_radio call TFAR_fnc_getAdditionalLrChannel} else {_radio call TFAR_fnc_getLrChannel};
        } else {
            _currentChannel = if (_isAdditional) then {_radio call TFAR_fnc_getAdditionalSwChannel} else {_radio call TFAR_fnc_getSwChannel};
        };

    } else { //Coming from our transmit code, reuse _radioChannel that came from its scope as our script doesn't use active channel
        _currentChannel = _radioChannel;
    };




    ["Radio.Transmit", [TRANSFORM_LR_RADIO_TO_EXT(_radio), _currentChannel, _start]] call TC_main_fnc_sendMessage;
};

private _evtOnSWChannelSet = {
    params ["", "_radio", "", ""];

    [_radio] call TC_main_fnc_Radio_doUpdateRadio;
};

private _evtOnLRChannelSet = {
    params ["", "_radioObj", "_radioQualif", "", ""];

    [[_radioObj, _radioQualif]] call TC_main_fnc_Radio_doUpdateRadio;
};

private _evtOnRadiosReceived = {
    params ["", "_radios"];

    {
        private _radio = _x;
        [_radio] call TC_main_fnc_Radio_doUpdateRadio;
    } forEach _radios;

    [TC_main_fnc_Radio_checkDeletedRadios]call CBA_fnc_execNextFrame;
};

private _evtOnFrequencyChangedFromUI = {
    params ["", "_radio", "", ""];

    [_radio] call TC_main_fnc_Radio_doUpdateRadio;
    [TC_main_fnc_Radio_checkDeletedRadios]call CBA_fnc_execNextFrame;
};


["TFAR_event_OnTangent", _evtOnTangent] call CBA_fnc_addEventHandler;
["TFAR_event_OnSWchannelSet", _evtOnSWChannelSet] call CBA_fnc_addEventHandler;
["TFAR_event_OnLRchannelSet", _evtOnLRChannelSet] call CBA_fnc_addEventHandler;
["TFAR_event_OnRadiosReceived", _evtOnRadiosReceived] call CBA_fnc_addEventHandler;
["TFAR_event_OnFrequencyChangedFromUI", _evtOnFrequencyChangedFromUI] call CBA_fnc_addEventHandler;
