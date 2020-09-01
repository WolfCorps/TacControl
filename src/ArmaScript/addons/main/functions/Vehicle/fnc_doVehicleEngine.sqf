
params ["_vehicle", "_engine"];

systemChat str _this;
if (_engine) then {
    player action ["engineOn", _vehicle];
} else {
    player action ["engineOff", _vehicle];
};
//Update is in EH
