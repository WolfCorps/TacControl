#include "script_component.hpp"

private _LRradios = TFAR_currentUnit call TFAR_fnc_LRRadiosList;
private _radios = TFAR_currentUnit call TFAR_fnc_radiosList;

if (isNil "_radios" || isNil "_LRradios") exitWith {};

//diag_log ["T1", _LRradios, _radios, TC_TFAR_Radios];
private _allRadios = _radios + _LRradios;

private _newRadios = _allRadios - TC_TFAR_Radios;
private _deletedRadios = TC_TFAR_Radios - _allRadios;

//diag_log ["T2", _newRadios, _deletedRadios];

{
    private _radio = _x;

    ["Radio.RadioDelete", [TRANSFORM_LR_RADIO_TO_EXT(_radio)]] call TC_main_fnc_sendMessage;
    TC_TFAR_Radios deleteAt (TC_TFAR_Radios find _x);
} forEach _deletedRadios;

{
    [_x] call TC_main_fnc_Radio_doUpdateRadio;
} forEach _newRadios;
