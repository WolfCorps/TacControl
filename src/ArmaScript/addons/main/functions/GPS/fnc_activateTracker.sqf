#include "script_component.hpp"
/*
 * Arguments:
 * 0: Unit <OBJECT>
 * 1: Explosive <OBJECT>
 * 2: Magazine classname <STRING>
 *
 * Return Value:
 * None

 * Public: Yes
 */

params ["", "_explosive", ""];

//Will prevent Minedetector beep
player action ["deactivateMine", player, _explosive];

if (!isServer) exitWith {
    _this remoteExec ["TC_main_fnc_GPS_activateTracker", 2];
};

TC_GPSTrackers = TC_GPSTrackers - [objNull];
TC_GPSTrackers pushBackUnique _explosive;
publicVariable "TC_GPSTrackers";
remoteExec ["TC_main_fnc_GPS_onServerTracker", 0];
