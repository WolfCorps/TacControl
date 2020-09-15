#include "script_component.hpp"

if (!(uiNamespace getVariable [QGVAR(hasMarkerTypes), false])) then {

    private _markerTypes = [];

    {
        private _className = configName _x;
        private _name = getText (_x >> "name");
        private _color = getArray (_x >> "color") apply {_x call BIS_fnc_parseNumber};
        private _size = getNumber (_x >> "size");
        private _shadow = getNumber (_x >> "shadow");
        private _icon = getText (_x >> "icon");

        private _markerTypeInfo = [_className, _name, _color, _size, _icon, _shadow];
        _markerTypes pushBack (_markerTypeInfo joinString toString[10]);

    } forEach ("true" configClasses (configFile >> "CfgMarkers"));

    private _markerColors = [];
    {
        private _className = configName _x;
        private _name = getText (_x >> "name");
        private _color = getArray (_x >> "color") apply {_x call BIS_fnc_parseNumber};


        private _markerTypeInfo = [_className, _name, _color];
        _markerColors pushBack (_markerTypeInfo joinString toString[10]);

    } forEach ("true" configClasses (configFile >> "CfgMarkerColors"));

    private _markerBrushes = [];
    {
        private _className = configName _x;
        private _name = getText (_x >> "name");
        private _texture = getText (_x >> "texture");
        private _drawBorder = getNumber (_x >> "drawBorder");

        private _markerTypeInfo = [_className, _name, _texture, _drawBorder];
        _markerBrushes pushBack (_markerTypeInfo joinString toString[10]);

    } forEach ("true" configClasses (configFile >> "CfgMarkerBrushes"));

    ["Marker.MarkerTypes", [
        _markerTypes joinString toString [9],
        _markerColors joinString toString [9],
        _markerBrushes joinString toString [9]
    ]] call TC_main_fnc_sendMessage;

    uiNamespace setVariable [QGVAR(hasMarkerTypes), true];
};


//["created", TC_main_fnc_Marker_onMarkerCreated] call CBA_fnc_addMarkerEventHandler;
//["deleted", TC_main_fnc_Marker_onMarkerDeleted] call CBA_fnc_addMarkerEventHandler;


["ace_markers_setMarkerPosLocal", TC_main_fnc_Marker_onMarkerPosChanged] call CBA_fnc_addEventHandler;
["ace_markers_markerPlaced", {
    params ["_newestMarker", "_editingMarker"];

    //systemChat str ["placed", _this];
    _this call TC_main_fnc_Marker_onMarkerCreated;

}] call CBA_fnc_addEventHandler;


["ace_markers_setMarkerNetwork", {
    //https://github.com/acemod/ACE3/blob/master/addons/markers/functions/fnc_setMarkerNetwork.sqf
    //params ["_newestMarker", "_editingMarker"];

    //systemChat str ["netw", _this];

}] call CBA_fnc_addEventHandler;


addMissionEventHandler ["MarkerCreated", TC_main_fnc_Marker_onMarkerCreated];
addMissionEventHandler ["MarkerUpdated", TC_main_fnc_Marker_onMarkerPosChanged];
addMissionEventHandler ["MarkerDeleted", TC_main_fnc_Marker_onMarkerDeleted];
