#include "script_component.hpp"


//Add TFAR eventhandlers
diag_log "######TC_radio_preInit";

private _evtOnTangent = {
    params ["_unit", "_radio", "_type", "_isAdditional", "_start"];

    //#TODO LR Radio support
    if (_radio isEqualType []) exitWith {};


    private _currentChannel = _radio call TFAR_fnc_getSwChannel;

    ["Radio.Transmit", [_radio, _currentChannel, _start]] call TC_main_fnc_sendMessage;
};


private _evtOnSWchannelSet = {
    params ["_unit", "_radio", "_channel", "_isAdditional"];

    //#TODO LR Radio support
    if (_radio isEqualType []) exitWith {};


    private _currentChannel = _radio call TFAR_fnc_getSwChannel;
    private _frequency = [_radio, _currentChannel + 1] call TFAR_fnc_getChannelFrequency;

    ["Radio.RadioUpdate", [_radio, _channel, _frequency]] call TC_main_fnc_sendMessage;
};





["TFAR_event_OnTangent", _evtOnTangent] call CBA_fnc_addEventHandler;
["TFAR_event_OnSWchannelSet", _evtOnSWchannelSet] call CBA_fnc_addEventHandler;
