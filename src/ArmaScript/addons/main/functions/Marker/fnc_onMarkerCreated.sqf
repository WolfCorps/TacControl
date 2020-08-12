#include "script_component.hpp"

params ["_markerName"];

systemChat str ["created", _this];

["Marker.Create", _markerName call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;
