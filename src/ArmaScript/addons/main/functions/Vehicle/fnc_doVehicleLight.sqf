
params ["_vehicle", "_light"];


if (_light) then {
    player action ["LightOn", _vehicle];
} else {
    player action ["LightOff", _vehicle];
};
["Vehicle.Update", ["LightOn", _light]] call TC_main_fnc_sendMessage;
