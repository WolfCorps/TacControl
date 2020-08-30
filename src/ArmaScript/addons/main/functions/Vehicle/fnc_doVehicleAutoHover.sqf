
params ["_vehicle", "_hover"];


if (_hover) then {
    _vehicle action ["autoHover", _vehicle];
} else {
    _vehicle action ["autoHoverCancel", _vehicle];
};
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
