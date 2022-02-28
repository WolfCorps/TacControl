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

params ["_explosive", ["_trackerName", ""]];

//Will prevent Minedetector beep
player action ["deactivateMine", player, _explosive];

#ifndef TC_OfflineMode
if (!isServer) exitWith {
    _this remoteExec ["TC_main_fnc_GPS_activateTracker", 2];
};
#endif

if (_trackerName == "") then {
    private _attachedTo = attachedTo _explosive;
    if (!isNull _attachedTo) then {
        _trackerName = format["GPS %1-%2", if (isPlayer _attachedTo) then {name _attachedTo} else {getText (configOf _attachedTo >> "displayName")}, GVAR(TrackerCounter)];
    } else {
    _trackerName = format["GPS %1-%2", name player, GVAR(TrackerCounter)];
    };
    GVAR(TrackerCounter) = GVAR(TrackerCounter) + 1;
};


//#TODO make this better? can't set var on projectile, can't use Hash as needs to be JIP global, CBA namespace? but needs serverside
missionNamespace setVariable ["TC_gt_"+(_explosive call BIS_fnc_netId), _trackerName, true];

TC_GPSTrackers = TC_GPSTrackers - [objNull];
TC_GPSTrackers pushBackUnique _explosive;
publicVariable "TC_GPSTrackers";
#if TC_OfflineMode
call TC_main_fnc_GPS_onServerTracker;
#else
remoteExec ["TC_main_fnc_GPS_onServerTracker", 0];
#endif
