
params ["_vehicle", "_brake"];

//Only RTD
if (_brake) then {
    _vehicle action ["WheelsBrakeOn", _vehicle];
} else {
    _vehicle action ["WheelsBrakeOff", _vehicle];
};
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
