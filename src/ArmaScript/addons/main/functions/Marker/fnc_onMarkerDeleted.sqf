#include "script_component.hpp"

params ["_markerName"];

systemChat str ["deleted", _this];
["Marker.Delete", [_markerName]] call TC_main_fnc_sendMessage;
