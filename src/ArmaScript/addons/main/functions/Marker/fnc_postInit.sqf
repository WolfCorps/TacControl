#include "script_component.hpp"


//Register existing markers
{_x call TC_main_fnc_Marker_onMarkerCreated} forEach allMapMarkers;

["Marker.PlayerId", [
    getPlayerId player
]] call TC_main_fnc_sendMessage;
