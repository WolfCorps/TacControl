#include "script_component.hpp"
/*
 * Description:
	Unregister a Interest handler.
	If the interest was active, the leave handler will be called.

 * Arguments:
 * 0: Interest name <STRING>
 * 1: Unique Interest Handle <STRING>
 *
 * Return Value:
 * None

 * Public: Yes
 */
params["_interestName", "_handle"];

private _interestMap = GVAR(InterestMap) getOrDefault [_interestName, createHashMap, true];

if (_interestMap getOrDefault ["active", false]) then {
	// Interest is currently active, execute the leave function
	private _y = _interestMap getOrDefault [_handle, [{},{},nil]];
	(_y select 2) call (_y select false);
};

_interestMap deleteAt _handle;
