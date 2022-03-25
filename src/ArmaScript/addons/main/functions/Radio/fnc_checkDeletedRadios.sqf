#include "script_component.hpp"

// TFAR_fnc_LRRadiosList has infinite recursion if player is null, this happens when after mission end the PFH keeps running
if (isNull player) exitWith {};

private _LRradios = (TFAR_currentUnit call TFAR_fnc_LRRadiosList apply {_x + [netId (_x select 0)]}); // We need to seperately store netId in case object becomes objNull
private _radios = TFAR_currentUnit call TFAR_fnc_radiosList;

if (isNil "_radios" || isNil "_LRradios") exitWith {};

//diag_log ["T1", _LRradios, _radios, TC_TFAR_Radios];
private _allRadios = _radios + _LRradios;

private _newRadios = _allRadios - TC_TFAR_Radios;
private _deletedRadios = TC_TFAR_Radios - _allRadios;

//diag_log ["T2", _newRadios, _deletedRadios];

{
    private _radio = _x;

    if (_radio isEqualType [] && {isNull (_radio select 0)}) then {
        // Special handling for LR radio if it was actually deleted and became objNull, we cannot get NetId otherwise
        // our apply above inserted netId into 3rd element just for this
        ["Radio.RadioDelete", [format["%1 %2", (_radio select 2), (_radio select 1)]]] call TC_main_fnc_sendMessage;
    } else {
        ["Radio.RadioDelete", [TRANSFORM_LR_RADIO_TO_EXT(_radio)]] call TC_main_fnc_sendMessage;
    };

    TC_TFAR_Radios deleteAt (TC_TFAR_Radios find _x);
} forEach _deletedRadios;

{
    [_x] call TC_main_fnc_Radio_doUpdateRadio;
} forEach _newRadios;
