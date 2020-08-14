#include "script_component.hpp"

//["created", TC_main_fnc_Marker_onMarkerCreated] call CBA_fnc_addMarkerEventHandler;
["deleted", TC_main_fnc_Marker_onMarkerDeleted] call CBA_fnc_addMarkerEventHandler;


["ace_markers_setMarkerPosLocal", TC_main_fnc_Marker_onMarkerPosChanged] call CBA_fnc_addEventHandler;
["ace_markers_markerPlaced", {
    params ["_newestMarker", "_editingMarker"];

    //systemChat str ["placed", _this];
    _this call TC_main_fnc_Marker_onMarkerCreated;

}] call CBA_fnc_addEventHandler;


["ace_markers_setMarkerNetwork", {
    //https://github.com/acemod/ACE3/blob/master/addons/markers/functions/fnc_setMarkerNetwork.sqf
    //params ["_newestMarker", "_editingMarker"];

    //systemChat str ["netw", _this];

}] call CBA_fnc_addEventHandler;
