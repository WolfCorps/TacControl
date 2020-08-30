
params ["_vehicle", "_gear"];


if (_gear) then {
    _vehicle action ["LandGear", _vehicle];
} else {
    _vehicle action ["LandGearUp", _vehicle];
};
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
