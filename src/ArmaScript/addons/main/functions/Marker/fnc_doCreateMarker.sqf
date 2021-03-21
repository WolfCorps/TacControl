#include "script_component.hpp"

params ["_arguments"];


_arguments params [
    "_markerName",
    "_markerType",
    "_markerColor",
    "_markerDir",
    "_markerPos",
    "_markerText",
    "_markerShape",
    "_markerAlpha",
    "_markerBrush",
    "_markerSize",
    "_markerChannel",
    "_markerPolyline"];

_markerDir = parseNumber _markerDir; // "1" -> 1
_markerPos = parseSimpleArray _markerPos; // "[1,2,3]" -> [1,2,3]
_markerAlpha = parseNumber _markerAlpha; // "1" -> 1
_markerSize = parseSimpleArray _markerSize; // "[1,1]" -> [1,1]
_markerChannel = parseNumber _markerChannel; // "1" -> 1
_markerPolyline = parseSimpleArray _markerPolyline; // "[[1,2],[1,2]]" -> [[1,2],[1,2]]


//Create local first to save some network traffic

isNil { //Critical section, to prevent markerUpdated EH from spamming updates
GVAR(ignoreMarkerUpdate) = true;
_marker = createMarkerLocal [_markerName, _markerPos, _markerChannel, player];

if (_markerType != "") then { _marker setMarkerTypeLocal _markerType; };
if (_markerColor != "") then { _marker setMarkerColorLocal _markerColor; };
if (_markerDir != 0) then { _marker setMarkerDirLocal _markerDir; };
if (_markerShape != "") then { _marker setMarkerShapeLocal _markerShape; };
if (_markerAlpha != 1) then { _marker setMarkerAlphaLocal _markerAlpha; };
if (_markerBrush != "") then { _marker setMarkerBrushLocal _markerBrush; };
if !(_markerSize isEqualTo []) then { _marker setMarkerSizeLocal _markerSize; };
if (_markerPolyline isNotEqualTo [] && {count _markerPolyline > 3}) then { _marker setMarkerPolylineLocal _markerPolyline; };

_marker setMarkerText _markerText; //This last call will make it public and transfer over network
GVAR(ignoreMarkerUpdate) = false;

_marker call TC_main_fnc_Marker_onMarkerCreated;
};
