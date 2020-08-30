
params ["_vehicle", "_sensors"];


if (_sensors) then {
    _vehicle action ["ActiveSensorsOn", _vehicle];
} else {
    _vehicle action ["ActiveSensorsOff", _vehicle];
};
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
