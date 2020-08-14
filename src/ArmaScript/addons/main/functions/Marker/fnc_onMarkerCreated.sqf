#include "script_component.hpp"

params ["_markerName"];

//systemChat str ["created", _this];


//After ACE marker edit, the direction/angle isn't set yet when this is called
[{
    ["Marker.Create", _this call TC_main_fnc_Marker_assembleMarkerInfo] call TC_main_fnc_sendMessage;
}, _markerName] call CBA_fnc_execNextFrame;

