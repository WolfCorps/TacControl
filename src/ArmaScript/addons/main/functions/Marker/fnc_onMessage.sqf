#include "script_component.hpp"

params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "GetMarkerTypes": {
            //is done at prestart, just leftover for getPlayerID
            ["Marker.PlayerId", [
                getPlayerId player
            ]] call TC_main_fnc_sendMessage;
        };
        case "CreateMarker": {
            [_arguments] call TC_main_fnc_Marker_doCreateMarker;
        };
        case "DeleteMarker": {
            deleteMarker (_arguments select 0);
        };

    };


};
