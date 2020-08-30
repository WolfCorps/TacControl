#include "script_component.hpp"

params ["_vehicle", "_engineState"];

["Vehicle.Update", ["EngineOn", _engineState]] call TC_main_fnc_sendMessage;
