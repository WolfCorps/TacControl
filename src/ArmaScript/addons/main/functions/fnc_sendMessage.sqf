#include "script_component.hpp"

params ["_function", "_arguments"];

if (isDedicated) exitWith {};
"TacControl" callExtension [_function, _arguments];
