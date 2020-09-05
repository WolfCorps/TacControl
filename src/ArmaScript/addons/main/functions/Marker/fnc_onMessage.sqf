#include "script_component.hpp"

params ["_function", "_arguments"];


if (_function#0 == "Cmd") then {

    switch (_function#1) do {
        case "GetMarkerTypes": {
//#TODO do at preStart or extension load
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
                _markerBrushes joinString toString [9],
                getplayerid player
            ]] call TC_main_fnc_sendMessage;
        };
        case "CreateMarker": {
            [_arguments] call TC_main_fnc_Marker_doCreateMarker;
        };
        case "DeleteMarker": {
            deleteMarker (_arguments select 0);
        };

    };


};
