
#include "script_component.hpp"



["ace_interactMenuOpened", {
    // No need to run if there are no GPS trackers
    if (TC_GPSTrackers isEqualTo []) exitWith {};


    params ["_interactionType"];
    // Ignore self-interaction menu, when in vehicle
    if (_interactionType == 0 && {vehicle ACE_player == ACE_player}) then {
        // Show pick up actions on CfgAmmo's
        call TC_main_fnc_GPS_renderPickUpInteraction;
    };

}] call CBA_fnc_addEventHandler;
