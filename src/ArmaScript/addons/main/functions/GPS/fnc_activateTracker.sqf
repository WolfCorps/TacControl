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


TC_GPSTrackers pushBackUnique _explosive;
call TC_main_fnc_GPS_sendTrackerUpdate;


