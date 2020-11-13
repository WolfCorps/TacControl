#include "script_component.hpp"




//#TODO ACE module, map marker pointer, explosives



// GVAR(detonators) = [player] call ace_explosives_fnc_getDetonators;

//["loadout", {
//    call TC_main_fnc_ACE_updateAvailableExplosives; //#TODO move to addClacker handler https://github.com/acemod/ACE3/pull/7994
//}] call CBA_fnc_addPlayerEventHandler;


// need addClacker EH to work right
[] spawn {
    while {sleep 5; true} do TC_main_fnc_ACE_updateAvailableExplosives;
};







[{
    [TC_main_fnc_ACE_updateAvailableExplosives, [], 2] call CBA_fnc_waitAndExecute;
    true;
}] call ace_explosives_fnc_addDetonateHandler;