#include "script_component.hpp"

params ["_vehicle"];

private _updateArray = [];

//#TODO remove engineTemp?
//#TODO helicopterRTD
//#TODO wheel brake

#define ANIMSOURCES \
    XX(AltBaro) \
    XX(AltRadar) \
    XX(Gear) \
    XX(HorizonBank) \
    XX(HorizonDive) \
    XX(VertSpeed) \
    XX(EngineTemp) \
    XX(Rpm) \
    XX(Speed) \
    XX(RotorHFullyDestroyed) \
    XX(TailRotorHFullyDestroyed) \
    XX(ActiveSensorsOn)

#define CONCAT(a,b) a##b

#define XX(name) \
    _updateArray append [QUOTE(CONCAT(Anim,name)), _vehicle animationSourcePhase QUOTE(name)];

ANIMSOURCES

_updateArray append ["AutoHover", isAutoHoverOn _vehicle]; //hax, meh
_updateArray append ["AnimFuel", fuel _vehicle]; //someone being extra lazy here //#TODO remove "Anim", its not anim anymore


["Vehicle.Update", _updateArray] call TC_main_fnc_sendMessage;
