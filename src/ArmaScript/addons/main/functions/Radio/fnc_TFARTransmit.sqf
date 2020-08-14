#include "script_component.hpp"

params ["_radio", "_radioChannel", "_startTransmit"];

private _swStartTransmit = {
    if (((TF_tangent_lr_pressed or TF_tangent_sw_pressed)) or {!alive TFAR_currentUnit} or {!call TFAR_fnc_haveSWRadio}) exitWith {false};

    if (!call TFAR_fnc_isAbleToUseRadio) exitWith {call TFAR_fnc_unableToUseHint;false};

    if (!([_radio] call TFAR_fnc_RadioOn)) exitWith {false};

    private _depth = TFAR_currentUnit call TFAR_fnc_eyeDepth;
    private _isolatedAndInside = TFAR_currentUnit call TFAR_fnc_vehicleIsIsolatedAndInside;

    if !([  TFAR_currentUnit,
            _isolatedAndInside,
            [_isolatedAndInside,_depth] call TFAR_fnc_canSpeak,
            _depth
        ] call TFAR_fnc_canUseSWRadio
        ||
        {([_depth,_isolatedAndInside] call TFAR_fnc_canUseDDRadio)}
        ) exitWith {call TFAR_fnc_inWaterHint;false};

    ["OnBeforeTangent", [TFAR_currentUnit, _radio, 0, false, true]] call TFAR_fnc_fireEventHandlers;
    private _currentFrequency = [_radio, _radioChannel + 1] call TFAR_fnc_getChannelFrequency;

    private _hintText = format[
                                localize "STR_TFAR_CORE_transmit",
                                format [
                                        "%1<img size='1.5' image='%2'/>",
                                        ([_radio, "displayName", ""] call TFAR_fnc_getWeaponConfigProperty) select [0, 40], //#TODO MAX_RADIONAME_LEN
                                        ([_radio, "picture", ""] call TFAR_fnc_getWeaponConfigProperty)
                                        ],
                                _radioChannel + 1,
                                _currentFrequency
                            ];

    private _pluginCommand = format[
                                    "TANGENT	PRESSED	%1%2	%3	%4	%5",
                                    _currentFrequency,
                                    _radio call TFAR_fnc_getSwRadioCode,
                                    ([_radio, "tf_range", 0] call TFAR_fnc_getWeaponConfigProperty) * (call TFAR_fnc_getTransmittingDistanceMultiplicator),
                                    ([_radio, "tf_subtype", "digital"] call TFAR_fnc_getWeaponConfigProperty),
                                    _radio
                                ];

    [_hintText,_pluginCommand, [0,-1] select TFAR_showTransmittingHint] call TFAR_fnc_processTangent;

    TF_tangent_sw_pressed = true;
    //						unit, radio, radioType, additional, buttonDown
    ["OnTangent", [TFAR_currentUnit, _radio, 0, false, true]] call TFAR_fnc_fireEventHandlers;
};


private _swStopTransmit = {
    if ((!TF_tangent_sw_pressed) or {!alive TFAR_currentUnit}) exitWith {false};

    ["OnBeforeTangent", [TFAR_currentUnit, _radio, 0, false, false]] call TFAR_fnc_fireEventHandlers;

    private _currentFrequency = [_radio, _radioChannel + 1] call TFAR_fnc_getChannelFrequency;
    private _hintText = format[
                            localize "STR_TFAR_CORE_transmit_end",
                            format [
                                    "%1<img size='1.5' image='%2'/>",
                                    ([_radio, "displayName", ""] call TFAR_fnc_getWeaponConfigProperty) select [0, 40],  //#TODO MAX_RADIONAME_LEN
                                    ([_radio, "picture", ""] call TFAR_fnc_getWeaponConfigProperty)
                                ],
                            _radioChannel + 1,
                            _currentFrequency
                        ];

    private _pluginCommand = format[
                                "TANGENT	RELEASED	%1%2	%3	%4",
                                _currentFrequency,
                                _radio call TFAR_fnc_getSwRadioCode,
                                ([_radio, "tf_range", 0] call TFAR_fnc_getWeaponConfigProperty) * (call TFAR_fnc_getTransmittingDistanceMultiplicator),
                                ([_radio, "tf_subtype", "digital"] call TFAR_fnc_getWeaponConfigProperty)
                            ];

    [_hintText,_pluginCommand, [0,nil] select TFAR_showTransmittingHint] call TFAR_fnc_processTangent;

    TF_tangent_sw_pressed = false;
    //						unit, radio, radioType, additional, buttonDown
    ["OnTangent", [TFAR_currentUnit, _radio, 0, false, false]] call TFAR_fnc_fireEventHandlers;
};

private _lrStartTransmit = {
    if (time - TF_last_lr_tangent_press < 0.1) exitWith {TF_last_lr_tangent_press = time;false};
    TF_last_lr_tangent_press = time;
    if ((TF_tangent_lr_pressed or TF_tangent_sw_pressed) or {!alive TFAR_currentUnit} or {!call TFAR_fnc_haveLRRadio}) exitWith {false};

    if (!isMultiplayer) exitWith {_x = localize "STR_TFAR_CORE_WM_Singleplayer";systemChat _x;hint _x;false};

    if (!call TFAR_fnc_isAbleToUseRadio) exitWith {call TFAR_fnc_unableToUseHint;false};
    if (!([_radio] call TFAR_fnc_RadioOn)) exitWith {false};

    if !([  TFAR_currentUnit,
            TFAR_currentUnit call TFAR_fnc_vehicleIsIsolatedAndInside,
            TFAR_currentUnit call TFAR_fnc_eyeDepth
        ] call TFAR_fnc_canUseLRRadio) exitWith {call TFAR_fnc_inWaterHint;false};

    ["OnBeforeTangent", [TFAR_currentUnit, _radio, 1, false, true]] call TFAR_fnc_fireEventHandlers;

    private _currentFrequency = [_radio, _radioChannel + 1] call TFAR_fnc_getChannelFrequency;

    private _hintText = format[
                                localize "STR_TFAR_CORE_transmit",
                                format ["%1<img size='1.5' image='%2'/>",
                                        [_radio select 0, "displayName"] call TFAR_fnc_getLrRadioProperty,
                                        getText(configFile >> "CfgVehicles" >> typeOf (_radio select 0) >> "picture")
                                        ],
                                _radioChannel + 1,
                                _currentFrequency
                            ];
    private _pluginCommand = format[
                                    "TANGENT_LR	PRESSED	%1%2	%3	%4	%5",
                                    _currentFrequency,
                                    _radio call TFAR_fnc_getLrRadioCode,
                                    ([_radio select 0, "tf_range"] call TFAR_fnc_getLrRadioProperty)  * (call TFAR_fnc_getTransmittingDistanceMultiplicator),
                                    [_radio select 0, "tf_subtype", "digital_lr"] call TFAR_fnc_getLrRadioProperty,
                                    typeOf (_radio select 0)
                                ];

    [_hintText, _pluginCommand, [0,-1] select TFAR_showTransmittingHint] call TFAR_fnc_processTangent;
    TF_tangent_lr_pressed = true;
    //				unit, radio, radioType, additional, buttonDown
    ["OnTangent", [TFAR_currentUnit, _radio, 1, false, true]] call TFAR_fnc_fireEventHandlers;
};

private _lrStopTransmit = {
    if (!(TF_tangent_lr_pressed) or {!alive TFAR_currentUnit}) exitWith {false};
    ["OnBeforeTangent", [TFAR_currentUnit, _radio, 1, false, false]] call TFAR_fnc_fireEventHandlers;

    private _currentFrequency = [_radio, _radioChannel + 1] call TFAR_fnc_getChannelFrequency;

    private _hintText = format[
                                localize "STR_TFAR_CORE_transmit_end",
                                format [
                                    "%1<img size='1.5' image='%2'/>",
                                    [_radio select 0, "displayName"] call TFAR_fnc_getLrRadioProperty,
                                    getText(configFile >> "CfgVehicles"  >> typeOf (_radio select 0) >> "picture")
                                ],
                                _radioChannel + 1,
                                _currentFrequency
                            ];

    private _pluginCommand = format[
                                    "TANGENT_LR	RELEASED	%1%2	%3	%4",
                                    _currentFrequency,
                                    _radio call TFAR_fnc_getLrRadioCode,
                                    ([_radio select 0, "tf_range", 0] call TFAR_fnc_getLrRadioProperty) * (call TFAR_fnc_getTransmittingDistanceMultiplicator),
                                    [_radio select 0, "tf_subtype", "digital_lr"] call TFAR_fnc_getLrRadioProperty
                                ];

    [_hintText,_pluginCommand, [0,nil] select TFAR_showTransmittingHint] call TFAR_fnc_processTangent;
    TF_tangent_lr_pressed = false;
    //						unit, radio, radioType, additional, buttonDown
    ["OnTangent", [TFAR_currentUnit, _radio, 1, false, false]] call TFAR_fnc_fireEventHandlers;
    false
};

if (_radio isEqualType []) then {
    if (_startTransmit) then _lrStartTransmit else _lrStopTransmit;
} else {
    if (_startTransmit) then _swStartTransmit else _swStopTransmit;
}



