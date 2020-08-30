
params ["_vehicle", "_light"];


if (_light) then {
    player action ["LightOn", _vehicle];
} else {
    player action ["LightOff", _vehicle];
};
//#TODO update state
