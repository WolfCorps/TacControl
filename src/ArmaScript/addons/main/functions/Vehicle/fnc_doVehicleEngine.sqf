
params ["_vehicle", "_engine"];


if (_engine) then {
    player action ["engineOn", _vehicle];
} else {
    player action ["engineOff", _vehicle];
};
//Update is in EH
