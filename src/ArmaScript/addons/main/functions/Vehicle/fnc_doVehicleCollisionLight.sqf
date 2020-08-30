
params ["_vehicle", "_light"];

//#TODO searchlight SearchLightOn/SearchLightOff
if (_light) then {
    player action ["CollisionLightOn", _vehicle];
} else {
    player action ["CollisionLightOff", _vehicle];
};
//#TODO update state
