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

params ["_unit", "_explosive", "_magazineClass"];

if(!isServer) exitWith {
    _this remoteExec ["TC_main_fnc_GPS_activateTracker", 2];
};

TC_GPSTrackers = TC_GPSTrackers - [objNull];
TC_GPSTrackers pushBackUnique _explosive;
publicVariable "TC_GPSTrackers";
_this remoteExec ["TC_main_fnc_GPS_onServerTracker", -2];



