#include "script_component.hpp"
/*
 * Description:
    Fires Interst handlers for _interestName's transition to _newState

 * Arguments:
 * 0: Interest name <STRING>
 * 1: ActiveNow <BOOL> - Did Interest just become active
 *
 * Return Value:
 * None

 * Public: No
 */

params ["_interestName", "_newState"];

private _interestMap = GVAR(InterestMap) getOrDefault [_interestName, createHashMap, true];
_interestMap set ["active", _newState];

{
	// Type checking is basically only to skip the "active" flag, thats annoying but meh
    _y isEqualType [] && {
        // [onLeave, onEnter, args]
        (_y select 2) call (_y select _newState)
    };
} forEach _interestMap;

