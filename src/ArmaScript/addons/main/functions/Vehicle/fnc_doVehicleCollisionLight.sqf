
params ["_vehicle", "_light"];

//#TODO searchlight SearchLightOn/SearchLightOff
_vehicle setCollisionLight _light;
//if (_light) then {
//    player action ["CollisionLightOn", _vehicle];
//} else {
//    player action ["CollisionLightOff", _vehicle];
//};
["Vehicle.Update", ["CollisionLight", _light]] call TC_main_fnc_sendMessage;
