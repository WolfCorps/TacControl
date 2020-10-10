#include "script_component.hpp"

params ["_marker", "_finalPos"];

//systemChat str ["pos", _this];
//systemChat str ["mkinf", _marker call TC_main_fnc_Marker_assembleMarkerInfo];
if (GVAR(ignoreMarkerUpdate)) exitWith {};

["Marker.Update", _marker call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;

