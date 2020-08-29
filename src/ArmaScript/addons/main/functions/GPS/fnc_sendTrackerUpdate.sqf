#include "script_component.hpp"





private _trackers = [];

//Remove deleted trackers
TC_GPSTrackers = TC_GPSTrackers - [objNull];

{
    private _trackerInfo = [];
    _trackerInfo pushBack (_x call BIS_fnc_netId);
    _trackerInfo pushBack getPos _x; //AGLS above sea or ground level
    _trackerInfo pushBack velocity _x;


    _trackerInfo = _trackerInfo joinString toString[10]; // "\n"

    _trackers pushBack _trackerInfo;
} forEach TC_GPSTrackers;




/*
  TrackerUpdate
    Array
      ID
      Position Vec3
      Velocity Vec3
*/

["GPS.TrackerUpdate", _trackers] call TC_main_fnc_sendMessage;
