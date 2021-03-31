
#include "script_component.hpp"
// https://github.com/acemod/ACE3/blob/8e7f9b6db53a279432a2372c04be311807d1193b/addons/advanced_throwing/functions/fnc_pickUp.sqf

params ["_helper", "_unit"];


private _activeTracker = _helper getVariable [QGVAR(tracker), objNull];

// Detach if attached (to vehicle for example or another player)
private _attachedTo = attachedTo _activeTracker;
if (!isNull _attachedTo) then {
    detach _activeTracker;
};


player addMagazine "TC_GPSTracker_Mag";
deleteVehicle _activeTracker;


TC_GPSTrackers = TC_GPSTrackers - [objNull];
publicVariable "TC_GPSTrackers";
remoteExec ["TC_main_fnc_GPS_onServerTracker", 0];

