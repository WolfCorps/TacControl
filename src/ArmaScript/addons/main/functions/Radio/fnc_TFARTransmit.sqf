params ["_radioClass", "_radioChannel", "_startTransmit"];

if (_startTransmit) then {
    if (((TF_tangent_lr_pressed or TF_tangent_sw_pressed)) or {!alive TFAR_currentUnit} or {!call TFAR_fnc_haveSWRadio}) exitWith {false};

    if (!call TFAR_fnc_isAbleToUseRadio) exitWith {call TFAR_fnc_unableToUseHint;false};

    private _radio = _radioClass;
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
} else {
    if ((!TF_tangent_sw_pressed) or {!alive TFAR_currentUnit}) exitWith {false};
    private _radio = _radioClass;

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
}

