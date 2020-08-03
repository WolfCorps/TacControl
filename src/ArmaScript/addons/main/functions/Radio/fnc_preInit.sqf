#include "script_component.hpp"


//Add TFAR eventhandlers


private _evtOnTangent = {
params ["_unit", "_radio", "_type", "_isAdditional", "_start"];

//#TODO LR Radio support
if (_radio isEqualType []) exitWith {};


private _currentChannel = _radio call TFAR_fnc_getSwChannel;

["Radio.RadioTransmit", [_radio, _currentChannel,_start]] call TC_main_fnc_sendMessage;




};







["TFAR_event_OnTangent", _evtOnX] call CBA_fnc_addEventHandler;