#include "script_component.hpp"

if (is3DEN || {getClientStateNumber < 10/* During briefing map screen */}) exitWith {["ImgDir.DoExport", []] call TC_main_fnc_sendMessage;};

[] spawn
{
   private _result = ["TacControl needs to export your Terrain information, this might freeze your game for about 30 seconds", "TacControl", "Yes, Export.", "No, don't do it"] call BIS_fnc_guiMessage;

   if (_result) then {
        ["ImgDir.DoExport", []] call TC_main_fnc_sendMessage;
   };
};
