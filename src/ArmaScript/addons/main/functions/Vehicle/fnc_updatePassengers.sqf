#include "script_component.hpp"

params ["_vehicle"];



/*


[
[drivers,...],
[codrivers,...],
[gunners,...],
[commanders,...]



]

*/

private _fullCrew = fullCrew _vehicle;

{
    //Object to name
    _x set [0, name (_x select 0)];
} forEach _fullCrew;

["Vehicle.CrewUpdate", _fullCrew apply {_x joinString toString[10]}] call TC_main_fnc_sendMessage;
