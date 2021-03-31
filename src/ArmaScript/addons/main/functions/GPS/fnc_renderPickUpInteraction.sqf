
#include "script_component.hpp"


// https://github.com/acemod/ACE3/blob/8e7f9b6db53a279432a2372c04be311807d1193b/addons/advanced_throwing/functions/fnc_renderPickUpInteraction.sqf


[{
    params ["_args", "_idPFH"];
    _args params ["_setPosition", "_addedPickUpHelpers", "_trackersHelped", "_nearTrackers"];

    // isNull is necessarry to prevent rare error when ending mission with interact key down
    if (ace_interact_menu_keyDown && {!isNull ACE_player}) then {
        // Rescan when player moved >5 meters from last pos, nearObjects can be costly with a lot of objects around
        if ((getPosASL ACE_player) distance _setPosition > 5) then {

            _nearTrackers = ACE_player nearObjects ["TC_GPSTracker_Ammo", 10];

            {
                if (!(_x in _trackersHelped)) then {

                    private _pickUpHelper = QGVAR(pickUpHelper) createVehicleLocal [0, 0, 0];

                    _pickUpHelper attachTo [_x, [0, 0, 0]];
                    _pickUpHelper setVariable [QGVAR(tracker), _x];

                    _addedPickUpHelpers pushBack _pickUpHelper;
                    _trackersHelped pushBack _x;
                };
                nil
            } count _nearTrackers;

            _args set [0, getPosASL ACE_player];
            _args set [3, _nearTrackers];
        };

        // Make sure helper is on correct location as it will not automatically move
        // attachTo is not supported with CfgAmmo, it is only used to get location
        {
            // Only handling with attachTo works nicely
            _x attachTo [_x getVariable [QGVAR(tracker), objNull], [0, 0, 0]];
            nil
        } count _addedPickUpHelpers;
    } else {
        {deleteVehicle _x} count _addedPickUpHelpers;
        [_idPFH] call CBA_fnc_removePerFrameHandler;
    };
}, 0, [(getPosASL ACE_player) vectorAdd [-100, 0, 0], [], [], []]] call CBA_fnc_addPerFrameHandler;
