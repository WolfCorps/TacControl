#include "script_component.hpp"

params ["_object1", "_rope", "_object2"];

["Vehicle.Update", ["SlingLoaded", !isNull getSlingLoad _object1]] call TC_main_fnc_sendMessage;
