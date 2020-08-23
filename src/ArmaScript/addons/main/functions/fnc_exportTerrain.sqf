#include "script_component.hpp"


[] spawn
{
   private _result = ["TacControl needs to export your Terrain information, this will freeze your game for about 30 seconds", "TacControl", "Yes, Export.", "No, don't do it"] call BIS_fnc_guiMessage;

   if (_result) then {
        ["BLABLA", []] call TC_main_fnc_sendMessage;
   };



   // Use _result here
};
