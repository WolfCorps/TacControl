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

params ["_explosive"];

#ifndef TC_OfflineMode
if (!isServer) exitWith {
    _this remoteExec ["TC_main_fnc_GPS_deactivateTracker", 2];
};
#endif

// clean up variable to save precious memory
missionNamespace setVariable ["TC_gt_"+(_explosive call BIS_fnc_netId), nil, true];

TC_GPSTrackers = TC_GPSTrackers - [objNull, _explosive];
publicVariable "TC_GPSTrackers";
#if TC_OfflineMode
call TC_main_fnc_GPS_onServerTracker;
#else
remoteExec ["TC_main_fnc_GPS_onServerTracker", 0];
#endif