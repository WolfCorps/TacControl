#include "script_component.hpp"

private _allExplosives = [player] call ACE_Explosives_fnc_getPlacedExplosives;
private _allDetonators = [player] call ACE_Explosives_fnc_getDetonators;
// object, fuseTime

// [
//    [44: rhsusf_m112x4_e.p3d,0.5,"Explosive code: 1","rhsusf_m112x4_mag","MK16_Transmitter"],
//    [48: c4_charge_small.p3d,0.5,"Explosive code: 2","DemoCharge_Remote_Mag","Command"]
// ]

private _hasDetonator = {
    params ["_trigger"];

    private _required = getArray (configFile >> "ACE_Triggers" >> _trigger >> "requires");

    _allDetonators findIf {_x in _required} != -1;
};


_allExplosives = _allExplosives select {
    _x params ["_obj", "_fuseTime", "_code", "_class", "_trigger"];

    _trigger call _hasDetonator;
};


_allExplosives = _allExplosives apply {
    _x params ["_obj", "_fuseTime", "_code", "_class", "_trigger"];

    private _required = getArray (configFile >> "ACE_Triggers" >> _trigger >> "requires");
    private _detonator = _allDetonators select (_allDetonators findIf {_x in _required});

    [_obj call BIS_fnc_netId, _code, _class, _detonator] joinString toString[10]; // "\n"
};


["ACE.AvailExp", _allExplosives] call TC_main_fnc_sendMessage;