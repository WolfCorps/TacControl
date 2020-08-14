#include "script_component.hpp"


TC_TFAR_Radios = [];

//Add TFAR eventhandlers

call TC_main_fnc_Radio_initTFAREvents;

[{

call TC_main_fnc_Radio_checkDeletedRadios;


}, 5] call CBA_fnc_addPerFrameHandler
