#include "script_component.hpp"

params ["_marker"];

//systemChat str ["pos", _this];
//systemChat str ["mkinf", _marker call TC_main_fnc_Marker_assembleMarkerInfo];
if (GVAR(ignoreMarkerUpdate)) exitWith {};

// filter invalid marker, I don't know why this happens but it does
if (markerColor _marker == "") exitWith {};

["Marker.Update", _marker call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;

