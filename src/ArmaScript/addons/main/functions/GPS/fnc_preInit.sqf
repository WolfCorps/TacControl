#include "script_component.hpp"

TC_GPSTrackers = [];

GVAR(TrackerCounter) = 1;


["ace_attach_attached", {
    params ["_attachedObject", "_itemClassname", "_temporary"];

    if (_itemClassname != "TC_GPSTracker_Mag") exitWith {};

    // if _temporary then its being temp reattached because player is exited a vehicle

    if (_temporary) then {
        // Attach a new tracker to the vehicle
        private _parentObject = attachedTo _attachedObject; // is player
        private _tempVehicleTracker = _parentObject getVariable [QGVAR(tempTracker), objNull];


        // activate the new tracker with the old name
        private _oldTrackerNetID = _tempVehicleTracker call BIS_fnc_netId;
        private _oldTrackerName = missionNamespace getVariable ["TC_gt_"+_oldTrackerNetID, _oldTrackerNetID];
        [_attachedObject, _oldTrackerName] call TC_main_fnc_GPS_activateTracker;

        // clean up the temp tracker we created
        [_tempVehicleTracker] call TC_main_fnc_GPS_deactivateTracker;
        deleteVehicle _tempVehicleTracker;
        _parentObject setVariable [QGVAR(tempTracker), nil];
    } else {
        [_attachedObject] call TC_main_fnc_GPS_activateTracker;
    }

}] call CBA_fnc_addEventHandler;



["ace_attach_detaching", {
    params ["_attachedObject", "_itemClassname", "_temporary"];

    if (_itemClassname != "TC_GPSTracker_Mag") exitWith {};

    // if _temporary then its being temp detached because player is getting into a vehicle, vehicle player is already updated

    if (_temporary) then {
        // Attach a new tracker to the vehicle
        private _parentObject = attachedTo _attachedObject; // is player

        private _oldTrackerNetID = _attachedObject call BIS_fnc_netId;
        private _oldTrackerName = missionNamespace getVariable ["TC_gt_"+_oldTrackerNetID, _oldTrackerNetID];
        // deactivate old one and clean up variables
        [_attachedObject] call TC_main_fnc_GPS_deactivateTracker;

        // create new temporary tracker and attach it to the new parent vehicle
        private _newTracker = "TC_ACE_Attach_GPSTracker" createVehicle [0,0,0];
        _newTracker attachTo [vehicle _parentObject, [0,0,0]]; //#TODO hide the tracker, or attach somewhere sensible

        [_newTracker, _oldTrackerName] call TC_main_fnc_GPS_activateTracker;
        _parentObject setVariable [QGVAR(tempTracker), _newTracker];
    } else {
        [_attachedObject] call TC_main_fnc_GPS_deactivateTracker;
    }






}] call CBA_fnc_addEventHandler;
