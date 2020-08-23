#include "script_component.hpp"

if (isDedicated) exitWith {};

//Server put a new tracker into TC_GPSTrackers
call TC_main_fnc_GPS_sendTrackerUpdate;
