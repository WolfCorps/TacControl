#include "script_component.hpp"

params ["_marker"];



[
    _marker,
    markerType _marker,
    markerColor _marker,
    markerDir _marker,
    markerPos _marker,
    markerText _marker,
    markerShape _marker,
    markerAlpha _marker,
    markerBrush _marker,
    markerSize _marker joinString ",",
    markerChannel _marker,
    markerPolyline _marker joinString ";"
]

