#include "script_component.hpp"
/*
 * Description:
    Registers a interest handler. If Interest is currently present, the enter handler will be called immediately.
    If Interest is not present it will be stored and executed when a Interest appears/disappears

 * Arguments:
 * 0: Interest name <STRING>
 * 1: OnActivate Handler <FUNCTION>
 * 2: OnDeactivate Handler <FUNCTION>
 *
 * Return Value:
 * Unique Interest Handle <STRING>

 * Public: Yes
 */
params["_interestName", "_functionOnEnter", "_functionOnLeave", ["_args", nil]];


private _interestMap = GVAR(InterestMap) getOrDefault [_interestName, createHashMap, true];

// Insert handler for later

private _uniqueHandlerId = hashValue [_functionOnEnter, _functionOnLeave, _args, random 1];
_interestMap set [_uniqueHandlerId, [_functionOnLeave, _functionOnEnter, _args]];

if (_interestMap getOrDefault ["active", false]) then {
    // Interest is already active, execute enter function right away
    _args call _functionOnEnter;
};

_uniqueHandlerId