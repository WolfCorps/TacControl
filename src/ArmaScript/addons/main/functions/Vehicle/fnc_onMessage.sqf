#include "script_component.hpp"

params ["_function", "_arguments"];


//#TODO actions

/*
		class ListLeftVehicleDisplay: ListRightVehicleDisplay
		{
			priority = 25;
			showWindow = 0;
			shortcut = "ListLeftVehicleDisplay";
			text = "Left Panel Next";
			textDefault = "Left Panel Next";
			hideActions[] = {"ListLeftVehicleDisplay"};
		};
		class ListRightVehicleDisplay: None
		{
			showWindow = 0;
			show = 0;
			shortcut = "ListRightVehicleDisplay";
			priority = 1;
			hideOnUse = 0;
			text = "Right Panel Next";
			textDefault = "Right Panel Next";
			hideActions[] = {"ListRightVehicleDisplay"};
		};
		class ListPrevLeftVehicleDisplay: ListPrevRightVehicleDisplay
		{
			priority = 25;
			showWindow = 0;
			shortcut = "ListPrevLeftVehicleDisplay";
			text = "Left Panel prev.";
			textDefault = "Left Panel prev.";
			hideActions[] = {"ListPrevLeftVehicleDisplay"};
		};
		class ListPrevRightVehicleDisplay: None
		{
			priority = 25;
			showWindow = 0;
			show = 0;
			hideOnUse = 0;
			shortcut = "ListPrevRightVehicleDisplay";
			text = "Right Panel prev.";
			textDefault = "Right Panel prev.";
			hideActions[] = {"ListPrevRightVehicleDisplay"};
		};
		class CloseLeftVehicleDisplay: CloseRightVehicleDisplay
		{
			priority = 25;
			showWindow = 0;
			show = 0;
			shortcut = "CloseLeftVehicleDisplay";
			text = "Close Left Panel";
			textDefault = "Close Left Panel";
			hideActions[] = {"CloseLeftVehicleDisplay"};
		};
		class CloseRightVehicleDisplay: None
		{
			priority = 25;
			showWindow = 0;
			show = 0;
			shortcut = "CloseRightVehicleDisplay";
			text = "Close Right Panel";
			textDefault = "Close Right Panel";
			hideActions[] = {"CloseRightVehicleDisplay"};
		};
		class NextModeLeftVehicleDisplay: NextModeRightVehicleDisplay
		{
			priority = 25;
			showWindow = 0;
			shortcut = "NextModeLeftVehicleDisplay";
			text = "Left Panel Mode";
			textDefault = "Left Panel Mode";
			hideActions[] = {"NextModeLeftVehicleDisplay"};
		};
		class NextModeRightVehicleDisplay: None
		{
			show = 0;
			shortcut = "NextModeRightVehicleDisplay";
			priority = 0.9;
			hideOnUse = 0;
			text = "Right Panel Mode";
			textDefault = "Right Panel Mode";
			showWindow = 0;
			hideActions[] = {"NextModeRightVehicleDisplay"};
		};

*/



if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "Gear": {

        };
        case "Engine": {

        };

    };


};
